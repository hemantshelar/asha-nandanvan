using System.Security.Claims;
using AshaNandanvan.Application.Cart;
using AshaNandanvan.Application.DogSitting;
using AshaNandanvan.Application.Offers;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Domain.Enums;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Web.Services;

public sealed class CartService : ICartService
{
    private const string StorageKey = "asha.cart";
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDogSittingService _dogSitting;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ProtectedLocalStorage _localStorage;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CartService(
        IDbContextFactory<AppDbContext> dbFactory,
        IDogSittingService dogSitting,
        AuthenticationStateProvider authenticationStateProvider,
        ProtectedLocalStorage localStorage)
    {
        _dbFactory = dbFactory;
        _dogSitting = dogSitting;
        _authenticationStateProvider = authenticationStateProvider;
        _localStorage = localStorage;
    }

    public event Action? Changed;

    public async Task<CartSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var userId = await GetUserIdAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            if (userId is not null)
            {
                await MergeGuestCartCoreAsync(db, userId, notify: false, cancellationToken);
                return await GetDbSnapshotAsync(db, userId, cancellationToken);
            }

            return await GetGuestSnapshotAsync(db);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AddAsync(
        int productId,
        int quantity = 1,
        int? slotId = null,
        DateTimeOffset? stayStart = null,
        DateTimeOffset? stayEnd = null,
        string? petName = null,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("That product is not available.");

            ProductSlot? slot = null;
            if (product.Category == ProductCategory.DogSitting)
            {
                if (stayStart is null || stayEnd is null)
                {
                    throw new InvalidOperationException("Choose drop-off and pick-up times.");
                }

                if (string.IsNullOrWhiteSpace(petName))
                {
                    throw new InvalidOperationException("Tell us the dog's name before booking.");
                }

                petName = petName.Trim();

                var availability = await _dogSitting.CheckAvailabilityAsync(stayStart.Value, stayEnd.Value, quantity, cancellationToken);
                if (!availability.CanBook)
                {
                    throw new InvalidOperationException(availability.Message);
                }
            }
            else if (product.Category.RequiresBooking())
            {
                if (slotId is null)
                {
                    throw new InvalidOperationException("Choose a date before adding this to your basket.");
                }

                slot = await db.ProductSlots.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == slotId && s.ProductId == productId && s.IsActive, cancellationToken)
                    ?? throw new InvalidOperationException("That date is no longer available.");

                if (slot.EndsAt <= DateTimeOffset.UtcNow)
                {
                    throw new InvalidOperationException("That date has already passed.");
                }
            }

            var skipStock = product.Category == ProductCategory.DogSitting;
            var available = slot?.Remaining ?? product.Stock;
            var userId = await GetUserIdAsync();
            if (userId is not null)
            {
                var cart = await GetOrCreateDbCartAsync(db, userId, cancellationToken);
                var item = product.Category == ProductCategory.DogSitting
                    ? cart.Items.FirstOrDefault(i => i.ProductId == productId)
                    : cart.Items.FirstOrDefault(i => SameLine(i, productId, slotId));
                var next = product.Category == ProductCategory.DogSitting ? quantity : (item?.Quantity ?? 0) + quantity;
                if (!skipStock)
                {
                    EnsureStock(product.Unit, available, next);
                }

                if (item is null)
                {
                    cart.Items.Add(new CartItem
                    {
                        ProductId = productId,
                        ProductSlotId = slotId,
                        StayStartsAt = stayStart,
                        StayEndsAt = stayEnd,
                        PetName = petName,
                        Quantity = next
                    });
                }
                else
                {
                    item.Quantity = next;
                    item.ProductSlotId = slotId;
                    item.StayStartsAt = stayStart;
                    item.StayEndsAt = stayEnd;
                    item.PetName = petName;
                }

                cart.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var lines = (await ReadGuestLinesAsync()).ToList();
                var existing = product.Category == ProductCategory.DogSitting
                    ? lines.FirstOrDefault(l => l.ProductId == productId)
                    : lines.FirstOrDefault(l => SameLine(l, productId, slotId));
                var next = product.Category == ProductCategory.DogSitting ? quantity : (existing?.Quantity ?? 0) + quantity;
                if (!skipStock)
                {
                    EnsureStock(product.Unit, available, next);
                }

                var line = new GuestLine(productId, next, slotId, stayStart, stayEnd, petName);
                if (existing is null)
                {
                    lines.Add(line);
                }
                else
                {
                    lines[lines.IndexOf(existing)] = line;
                }

                await WriteGuestLinesAsync(lines);
            }
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke();
    }

    public async Task UpdateQuantityAsync(int productId, int quantity, int? slotId = null, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            await RemoveAsync(productId, slotId, cancellationToken);
            return;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var userId = await GetUserIdAsync();
            if (userId is not null)
            {
                await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
                var cart = await GetOrCreateDbCartAsync(db, userId, cancellationToken);
                var item = cart.Items.FirstOrDefault(i => SameLine(i, productId, slotId));
                if (item is not null)
                {
                    item.Quantity = quantity;
                    cart.UpdatedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                var lines = (await ReadGuestLinesAsync()).ToList();
                var index = lines.FindIndex(l => SameLine(l, productId, slotId));
                if (index >= 0)
                {
                    lines[index] = lines[index] with { Quantity = quantity };
                    await WriteGuestLinesAsync(lines);
                }
            }
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke();
    }

    public async Task RemoveAsync(int productId, int? slotId = null, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var userId = await GetUserIdAsync();
            if (userId is not null)
            {
                await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
                var cart = await GetOrCreateDbCartAsync(db, userId, cancellationToken);
                var item = cart.Items.FirstOrDefault(i => SameLine(i, productId, slotId));
                if (item is not null)
                {
                    cart.Items.Remove(item);
                    cart.UpdatedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                var lines = (await ReadGuestLinesAsync()).Where(l => !SameLine(l, productId, slotId)).ToList();
                await WriteGuestLinesAsync(lines);
            }
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke();
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var userId = await GetUserIdAsync();
            if (userId is not null)
            {
                await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
                var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
                if (cart is not null)
                {
                    cart.Items.Clear();
                    cart.UpdatedAt = DateTimeOffset.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                }
            }

            await WriteGuestLinesAsync([]);
        }
        finally
        {
            _gate.Release();
        }

        Changed?.Invoke();
    }

    public async Task MergeGuestCartAsync(CancellationToken cancellationToken = default)
    {
        var notify = false;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var userId = await GetUserIdAsync();
            if (userId is null)
            {
                return;
            }

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            notify = await MergeGuestCartCoreAsync(db, userId, notify: false, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }

        if (notify)
        {
            Changed?.Invoke();
        }
    }

    private async Task<bool> MergeGuestCartCoreAsync(
        AppDbContext db,
        string userId,
        bool notify,
        CancellationToken cancellationToken)
    {
        var guestLines = await ReadGuestLinesAsync();
        if (guestLines.Count == 0)
        {
            return false;
        }

        var cart = await GetOrCreateDbCartAsync(db, userId, cancellationToken);
        foreach (var line in guestLines)
        {
            var existing = cart.Items.FirstOrDefault(i => SameLine(i, line.ProductId, line.SlotId));
            if (existing is null)
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = line.ProductId,
                    ProductSlotId = line.SlotId,
                    StayStartsAt = line.StayStart,
                    StayEndsAt = line.StayEnd,
                    PetName = line.PetName,
                    Quantity = line.Quantity
                });
            }
            else
            {
                existing.Quantity += line.Quantity;
                if (!string.IsNullOrWhiteSpace(line.PetName))
                {
                    existing.PetName = line.PetName;
                }
            }
        }

        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await WriteGuestLinesAsync([]);

        if (notify)
        {
            Changed?.Invoke();
        }

        return true;
    }

    private static async Task<CartSnapshot> GetDbSnapshotAsync(AppDbContext db, string userId, CancellationToken cancellationToken)
    {
        var cart = await db.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .Include(c => c.Items)
            .ThenInclude(i => i.ProductSlot)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is null)
        {
            return new CartSnapshot([]);
        }

        return new CartSnapshot(cart.Items.Select(ToLine).ToList());
    }

    private async Task<CartSnapshot> GetGuestSnapshotAsync(AppDbContext db)
    {
        var guestLines = await ReadGuestLinesAsync();
        if (guestLines.Count == 0)
        {
            return new CartSnapshot([]);
        }

        var ids = guestLines.Select(l => l.ProductId).Distinct().ToList();
        var products = await db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync();
        var slotIds = guestLines.Where(l => l.SlotId.HasValue).Select(l => l.SlotId!.Value).ToList();
        var slots = slotIds.Count == 0
            ? []
            : await db.ProductSlots.AsNoTracking().Where(s => slotIds.Contains(s.Id)).ToListAsync();

        var lines = guestLines
            .Select(l =>
            {
                var product = products.FirstOrDefault(p => p.Id == l.ProductId);
                if (product is null)
                {
                    return null;
                }

                var slot = l.SlotId is null ? null : slots.FirstOrDefault(s => s.Id == l.SlotId);
                return ToLine(product, slot, l.Quantity, l.StayStart, l.StayEnd, l.PetName);
            })
            .Where(l => l is not null)
            .Select(l => l!)
            .ToList();

        return new CartSnapshot(lines);
    }

    private static CartLine ToLine(CartItem item) =>
        ToLine(item.Product, item.ProductSlot, item.Quantity, item.StayStartsAt, item.StayEndsAt, item.PetName);

    private static CartLine ToLine(Product product, ProductSlot? slot, int quantity, DateTimeOffset? stayStart = null, DateTimeOffset? stayEnd = null, string? petName = null)
    {
        var label = stayStart is not null && stayEnd is not null
            ? BookingPricing.StayLabel(stayStart.Value, stayEnd.Value, petName)
            : slot is null ? null : BookingPricing.SlotLabel(slot, product.Category);

        return new(
            product.Id,
            slot?.Id,
            product.Name,
            product.Slug,
            product.Unit,
            BookingPricing.UnitPrice(product, slot, stayStart, stayEnd),
            quantity,
            slot?.Remaining ?? product.Stock,
            product.ImagePath,
            label);
    }

    private static async Task<Cart> GetOrCreateDbCartAsync(AppDbContext db, string userId, CancellationToken cancellationToken)
    {
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Carts.Add(cart);
        await db.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private async Task<string?> GetUserIdAsync()
    {
        var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true
            ? state.User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
    }

    private async Task<IReadOnlyList<GuestLine>> ReadGuestLinesAsync()
    {
        try
        {
            var result = await _localStorage.GetAsync<List<GuestLine>>(StorageKey);
            return result.Success && result.Value is not null ? result.Value : [];
        }
        catch (InvalidOperationException)
        {
            return [];
        }
    }

    private async Task WriteGuestLinesAsync(IReadOnlyList<GuestLine> lines)
    {
        try
        {
            await _localStorage.SetAsync(StorageKey, lines.ToList());
        }
        catch (InvalidOperationException)
        {
            // Prerender — the next interactive render will persist.
        }
    }

    private static void EnsureStock(string unit, int available, int next)
    {
        if (next > available)
        {
            throw new InvalidOperationException($"Only {available} {unit} available.");
        }
    }

    private static bool SameLine(CartItem item, int productId, int? slotId) =>
        item.ProductId == productId && item.ProductSlotId == slotId;

    private static bool SameLine(GuestLine line, int productId, int? slotId) =>
        line.ProductId == productId && line.SlotId == slotId;

    private sealed record GuestLine(
        int ProductId,
        int Quantity,
        int? SlotId = null,
        DateTimeOffset? StayStart = null,
        DateTimeOffset? StayEnd = null,
        string? PetName = null);
}
