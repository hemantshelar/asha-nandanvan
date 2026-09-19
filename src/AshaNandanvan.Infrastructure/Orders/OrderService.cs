using AshaNandanvan.Application.Orders;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Domain.Enums;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Infrastructure.Orders;

public sealed class OrderService : IOrderService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public OrderService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<OrderSummary> CreatePendingOrderAsync(string userId, CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var cart = await db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
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

            if (item.Quantity > item.Product.Stock)
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
            Status = OrderStatus.PendingPayment,
            Total = cart.Items.Sum(i => i.Product.Price * i.Quantity),
            PaymentProvider = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Unit = i.Product.Unit,
                Quantity = i.Quantity,
                UnitPrice = i.Product.Price
            }).ToList()
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return ToSummary(order);
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
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order was not found.");

        order.Status = status;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkPaidAsync(string orderNumber, string paymentReference, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

        if (order is null || order.Status != OrderStatus.PendingPayment)
        {
            return;
        }

        foreach (var line in order.Items)
        {
            var product = await db.Products.FirstAsync(p => p.Id == line.ProductId, cancellationToken);
            product.Stock = Math.Max(0, product.Stock - line.Quantity);
            product.UpdatedAt = DateTimeOffset.UtcNow;
        }

        order.Status = OrderStatus.Paid;
        order.PaymentReference = paymentReference;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        var carts = await db.Carts.Include(c => c.Items).Where(c => c.UserId == order.UserId).ToListAsync(cancellationToken);
        foreach (var cart in carts)
        {
            cart.Items.Clear();
            cart.UpdatedAt = DateTimeOffset.UtcNow;
        }

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
            order.Items.Select(i => new OrderLineSummary(i.ProductName, i.Unit, i.Quantity, i.UnitPrice)).ToList(),
            order.PaymentReference);
}
