using AshaNandanvan.Application.DogSitting;
using AshaNandanvan.Application.Offers;
using AshaNandanvan.Application.Orders;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Domain.Enums;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Infrastructure.Orders;

public sealed class OrderService : IOrderService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDogSittingService _dogSitting;

    public OrderService(IDbContextFactory<AppDbContext> dbFactory, IDogSittingService dogSitting)
    {
        _dbFactory = dbFactory;
        _dogSitting = dogSitting;
    }

    public async Task<OrderSummary> CreateOrderAsync(string userId, CheckoutRequest request, bool payNow, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var cart = await db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .Include(c => c.Items)
            .ThenInclude(i => i.ProductSlot)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Your basket is empty.");

        if (cart.Items.Count == 0)
        {
            throw new InvalidOperationException("Your basket is empty.");
        }

        foreach (var item in cart.Items)
        {
            if (!item.Product.IsActive)
            {
                throw new InvalidOperationException($"{item.Product.Name} is no longer available.");
            }

            if (item.Product.Category == ProductCategory.DogSitting)
            {
                if (item.StayStartsAt is null || item.StayEndsAt is null)
                {
                    throw new InvalidOperationException("Choose drop-off and pick-up for the dog sit.");
                }

                var availability = await _dogSitting.CheckAvailabilityAsync(
                    item.StayStartsAt.Value,
                    item.StayEndsAt.Value,
                    item.Quantity,
                    cancellationToken: cancellationToken);
                if (!availability.CanBook)
                {
                    throw new InvalidOperationException(availability.Message);
                }
            }
            else if (item.Product.Category.RequiresBooking())
            {
                if (item.ProductSlot is null || !item.ProductSlot.IsActive || item.ProductSlot.EndsAt <= DateTimeOffset.UtcNow)
                {
                    throw new InvalidOperationException($"{item.Product.Name} needs an open date. Remove it and choose again.");
                }

                if (item.Quantity > item.ProductSlot.Remaining)
                {
                    throw new InvalidOperationException($"Only {item.ProductSlot.Remaining} {item.Product.Unit} left on that date for {item.Product.Name}.");
                }
            }
            else if (item.Quantity > item.Product.Stock)
            {
                throw new InvalidOperationException($"Only {item.Product.Stock} {item.Product.Unit} of {item.Product.Name} left.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            OrderNumber = $"AN-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            UserId = userId,
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            PickupDate = request.PickupDate,
            PickupWindow = request.PickupWindow,
            Status = payNow ? OrderStatus.PendingPayment : OrderStatus.Placed,
            Total = cart.Items.Sum(i => BookingPricing.LineTotal(i.Product, i.ProductSlot, i.Quantity, i.StayStartsAt, i.StayEndsAt, i.IsTrialStay)),
            PaymentProvider = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Unit = i.Product.Unit,
                Quantity = i.Quantity,
                UnitPrice = BookingPricing.UnitPrice(i.Product, i.ProductSlot, i.StayStartsAt, i.StayEndsAt, i.IsTrialStay),
                ProductSlotId = i.ProductSlotId,
                StayStartsAt = i.StayStartsAt,
                StayEndsAt = i.StayEndsAt,
                PetName = i.PetName,
                PetBreed = i.PetBreed,
                IsTrialStay = i.IsTrialStay,
                IntendedStayStartsAt = i.IntendedStayStartsAt,
                IntendedStayEndsAt = i.IntendedStayEndsAt,
                SlotLabel = i.StayStartsAt is not null && i.StayEndsAt is not null
                    ? BookingPricing.StayLabel(i.StayStartsAt.Value, i.StayEndsAt.Value, i.PetName, i.PetBreed, i.IsTrialStay)
                    : i.ProductSlot is null ? null : BookingPricing.SlotLabel(i.ProductSlot, i.Product.Category)
            }).ToList()
        };

        db.Orders.Add(order);

        if (!payNow)
        {
            await ReserveInventoryAsync(db, order, cancellationToken);
            cart.Items.Clear();
            cart.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToSummary(order);
    }

    public async Task<IReadOnlyList<OrderSummary>> GetMineAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(ToSummary).ToList();
    }

    public async Task<OrderSummary?> GetByNumberAsync(string orderNumber, string? userId = null, bool admin = false, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Orders.AsNoTracking().Include(o => o.Items).Where(o => o.OrderNumber == orderNumber);
        if (!admin && userId is not null)
        {
            query = query.Where(o => o.UserId == userId);
        }

        var order = await query.FirstOrDefaultAsync(cancellationToken);
        return order is null ? null : ToSummary(order);
    }

    public async Task<IReadOnlyList<OrderSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(ToSummary).ToList();
    }

    public async Task UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order was not found.");

        if (status == OrderStatus.Paid && order.Status.IsUnpaid())
        {
            await MarkPaidCoreAsync(db, order, "admin", "Admin", cancellationToken);
        }
        else if (status is OrderStatus.Cancelled or OrderStatus.Rejected
            && order.Status is OrderStatus.Placed or OrderStatus.Paid or OrderStatus.Confirmed or OrderStatus.ReadyForPickup)
        {
            await RestoreInventoryAsync(db, order, cancellationToken);
            order.Status = status;
            order.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            order.Status = status;
            order.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var hasTrial = await db.OrderItems.AnyAsync(i => i.OrderId == orderId && i.IsTrialStay, cancellationToken);
        if (hasTrial)
        {
            throw new InvalidOperationException("Finish the trial night, then accept the original booking or reject it.");
        }

        await UpdateStatusAsync(orderId, OrderStatus.Confirmed, cancellationToken);
    }

    public Task RejectAsync(int orderId, CancellationToken cancellationToken = default) =>
        UpdateStatusAsync(orderId, OrderStatus.Rejected, cancellationToken);

    public async Task AcceptOriginalStayAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order was not found.");

        var summary = ToSummary(order);
        if (!summary.CanAcceptOriginalStay)
        {
            throw new InvalidOperationException("The trial night must finish before the original stay can be accepted.");
        }

        foreach (var line in order.Items.Where(i => i.IsTrialStay))
        {
            if (line.IntendedStayStartsAt is null || line.IntendedStayEndsAt is null)
            {
                throw new InvalidOperationException("This trial has no original stay dates stored.");
            }

            var availability = await _dogSitting.CheckAvailabilityAsync(
                line.IntendedStayStartsAt.Value,
                line.IntendedStayEndsAt.Value,
                line.Quantity,
                allowPastDropOff: true,
                cancellationToken: cancellationToken);
            if (!availability.CanBook)
            {
                throw new InvalidOperationException(availability.Message);
            }

            line.StayStartsAt = line.IntendedStayStartsAt;
            line.StayEndsAt = line.IntendedStayEndsAt;
            line.IsTrialStay = false;
            line.UnitPrice = BookingPricing.UnitPrice(
                line.Product,
                line.ProductSlot,
                line.StayStartsAt,
                line.StayEndsAt);
            line.SlotLabel = BookingPricing.StayLabel(
                line.StayStartsAt.Value,
                line.StayEndsAt.Value,
                line.PetName,
                line.PetBreed);
        }

        var firstStay = order.Items.FirstOrDefault(i => i.StayStartsAt is not null);
        if (firstStay?.StayStartsAt is DateTimeOffset dropOff)
        {
            var sydney = dropOff.ToOffset(BookingPricing.SydneyOffset(dropOff));
            order.PickupDate = DateOnly.FromDateTime(sydney.DateTime);
        }

        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        order.Status = string.IsNullOrWhiteSpace(order.PaymentReference)
            ? OrderStatus.Placed
            : OrderStatus.Paid;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkPaidAsync(string orderNumber, string paymentReference, string? provider = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

        if (order is null || !order.Status.IsUnpaid())
        {
            return;
        }

        await MarkPaidCoreAsync(db, order, paymentReference, provider, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AttachPaymentSessionAsync(string orderNumber, string provider, string? reference, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
        if (order is null)
        {
            return;
        }

        order.PaymentProvider = provider;
        if (!string.IsNullOrWhiteSpace(reference))
        {
            order.PaymentReference = reference;
        }

        order.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkPaidCoreAsync(
        AppDbContext db,
        Order order,
        string paymentReference,
        string? provider,
        CancellationToken cancellationToken)
    {
        if (order.Status == OrderStatus.PendingPayment)
        {
            await ReserveInventoryAsync(db, order, cancellationToken);
        }

        if (order.Status != OrderStatus.Confirmed)
        {
            order.Status = OrderStatus.Paid;
        }
        order.PaymentReference = paymentReference;
        if (!string.IsNullOrWhiteSpace(provider))
        {
            order.PaymentProvider = provider;
        }

        order.UpdatedAt = DateTimeOffset.UtcNow;

        var carts = await db.Carts.Include(c => c.Items).Where(c => c.UserId == order.UserId).ToListAsync(cancellationToken);
        foreach (var cart in carts)
        {
            cart.Items.Clear();
            cart.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private async Task ReserveInventoryAsync(AppDbContext db, Order order, CancellationToken cancellationToken)
    {
        foreach (var line in order.Items)
        {
            var product = await db.Products.FirstAsync(p => p.Id == line.ProductId, cancellationToken);
            if (product.Category.RequiresBooking() && line.ProductSlotId is int slotId)
            {
                var slot = await db.ProductSlots.FirstAsync(s => s.Id == slotId, cancellationToken);
                slot.BookedCount = Math.Min(slot.Capacity, slot.BookedCount + line.Quantity);
            }
            else if (product.Category == ProductCategory.DogSitting)
            {
                if (line.StayStartsAt is null || line.StayEndsAt is null)
                {
                    continue;
                }

                var availability = await _dogSitting.CheckAvailabilityAsync(
                    line.StayStartsAt.Value,
                    line.StayEndsAt.Value,
                    line.Quantity,
                    cancellationToken: cancellationToken);
                if (!availability.CanBook)
                {
                    throw new InvalidOperationException(availability.Message);
                }
            }
            else
            {
                product.Stock = Math.Max(0, product.Stock - line.Quantity);
            }

            product.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static async Task RestoreInventoryAsync(AppDbContext db, Order order, CancellationToken cancellationToken)
    {
        foreach (var line in order.Items)
        {
            var product = await db.Products.FirstAsync(p => p.Id == line.ProductId, cancellationToken);
            if (product.Category.RequiresBooking() && line.ProductSlotId is int slotId)
            {
                var slot = await db.ProductSlots.FirstAsync(s => s.Id == slotId, cancellationToken);
                slot.BookedCount = Math.Max(0, slot.BookedCount - line.Quantity);
            }
            else if (product.Category != ProductCategory.DogSitting)
            {
                product.Stock += line.Quantity;
            }

            product.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static OrderSummary ToSummary(Order order) =>
        new(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.Total,
            order.PickupDate,
            order.PickupWindow,
            order.CustomerName,
            order.CustomerEmail,
            order.Phone,
            order.CreatedAt,
            order.Items.Select(i => new OrderLineSummary(
                i.ProductName,
                i.Unit,
                i.Quantity,
                i.UnitPrice,
                i.SlotLabel,
                i.IsTrialStay,
                i.StayEndsAt,
                i.IntendedStayStartsAt,
                i.IntendedStayEndsAt)).ToList(),
            order.PaymentReference,
            order.PaymentProvider);
}
