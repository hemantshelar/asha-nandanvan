using System.Security.Claims;
using AshaNandanvan.Application.Cart;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Web.Services;

public sealed class CartService : ICartService
{
    private const string StorageKey = "asha.cart";
    private readonly AppDbContext _db;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ProtectedLocalStorage _localStorage;

    public CartService(
        AppDbContext db,
        AuthenticationStateProvider authenticationStateProvider,
        ProtectedLocalStorage localStorage)
    {
        _db = db;
        _authenticationStateProvider = authenticationStateProvider;
        _localStorage = localStorage;
    }

    public event Action? Changed;

    public async Task<CartSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var userId = await GetUserIdAsync();
        if (userId is not null)
        {
            await MergeGuestCartAsync(cancellationToken);
            return await GetDbSnapshotAsync(userId, cancellationToken);
        }

        return await GetGuestSnapshotAsync();
    }

    public async Task AddAsync(int productId, int quantity = 1, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("That product is not available.");

        var userId = await GetUserIdAsync();
        if (userId is not null)
        {
            var cart = await GetOrCreateDbCartAsync(userId, cancellationToken);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            var next = (item?.Quantity ?? 0) + quantity;
            if (next > product.Stock)
            {
                throw new InvalidOperationException($"Only {product.Stock} {product.Unit} available.");
            }

            if (item is null)
            {
                cart.Items.Add(new CartItem { ProductId = productId, Quantity = next });
            }
            else
            {
                item.Quantity = next;
            }

            cart.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            var lines = (await ReadGuestLinesAsync()).ToList();
            var existing = lines.FirstOrDefault(l => l.ProductId == productId);
            var next = (existing?.Quantity ?? 0) + quantity;
            if (next > product.Stock)
            {
                throw new InvalidOperationException($"Only {product.Stock} {product.Unit} available.");
            }

            if (existing is null)
            {
                lines.Add(new GuestLine(productId, next));
            }
            else
            {
                lines[lines.IndexOf(existing)] = existing with { Quantity = next };
            }

            await WriteGuestLinesAsync(lines);
        }

        Changed?.Invoke();
    }

    public async Task UpdateQuantityAsync(int productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            await RemoveAsync(productId, cancellationToken);
            return;
        }

        var userId = await GetUserIdAsync();
        if (userId is not null)
        {
            var cart = await GetOrCreateDbCartAsync(userId, cancellationToken);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item is not null)
            {
                item.Quantity = quantity;
                cart.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            var lines = (await ReadGuestLinesAsync()).ToList();
            var index = lines.FindIndex(l => l.ProductId == productId);
            if (index >= 0)
            {
                lines[index] = lines[index] with { Quantity = quantity };
                await WriteGuestLinesAsync(lines);
            }
        }

        Changed?.Invoke();
    }

    public async Task RemoveAsync(int productId, CancellationToken cancellationToken = default)
    {
        var userId = await GetUserIdAsync();
        if (userId is not null)
        {
            var cart = await GetOrCreateDbCartAsync(userId, cancellationToken);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item is not null)
            {
                cart.Items.Remove(item);
                cart.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            var lines = (await ReadGuestLinesAsync()).Where(l => l.ProductId != productId).ToList();
            await WriteGuestLinesAsync(lines);
        }

        Changed?.Invoke();
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var userId = await GetUserIdAsync();
        if (userId is not null)
        {
            var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            if (cart is not null)
            {
                cart.Items.Clear();
                cart.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        await WriteGuestLinesAsync([]);
        Changed?.Invoke();
    }

    public async Task MergeGuestCartAsync(CancellationToken cancellationToken = default)
    {
        var userId = await GetUserIdAsync();
        if (userId is null)
        {
            return;
        }

        var guestLines = await ReadGuestLinesAsync();
        if (guestLines.Count == 0)
        {
            return;
        }

        var cart = await GetOrCreateDbCartAsync(userId, cancellationToken);
        foreach (var line in guestLines)
        {
            var existing = cart.Items.FirstOrDefault(i => i.ProductId == line.ProductId);
            if (existing is null)
            {
                cart.Items.Add(new CartItem { ProductId = line.ProductId, Quantity = line.Quantity });
            }
            else
            {
                existing.Quantity += line.Quantity;
            }
        }

        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await WriteGuestLinesAsync([]);
        Changed?.Invoke();
    }

    private async Task<CartSnapshot> GetDbSnapshotAsync(string userId, CancellationToken cancellationToken)
    {
        var cart = await _db.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is null)
        {
            return new CartSnapshot([]);
        }

        var lines = cart.Items.Select(i => new CartLine(
            i.ProductId,
            i.Product.Name,
            i.Product.Slug,
            i.Product.Unit,
            i.Product.Price,
            i.Quantity,
            i.Product.Stock,
            i.Product.ImagePath)).ToList();

        return new CartSnapshot(lines);
    }

    private async Task<CartSnapshot> GetGuestSnapshotAsync()
    {
        var guestLines = await ReadGuestLinesAsync();
        if (guestLines.Count == 0)
        {
            return new CartSnapshot([]);
        }

        var ids = guestLines.Select(l => l.ProductId).ToList();
        var products = await _db.Products.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync();
        var lines = guestLines
            .Join(products, l => l.ProductId, p => p.Id, (l, p) => new CartLine(
                p.Id, p.Name, p.Slug, p.Unit, p.Price, l.Quantity, p.Stock, p.ImagePath))
            .ToList();

        return new CartSnapshot(lines);
    }

    private async Task<Cart> GetOrCreateDbCartAsync(string userId, CancellationToken cancellationToken)
    {
        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
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
        _db.Carts.Add(cart);
        await _db.SaveChangesAsync(cancellationToken);
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

    private sealed record GuestLine(int ProductId, int Quantity);
}
