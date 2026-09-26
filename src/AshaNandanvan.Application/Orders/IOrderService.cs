using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Application.Orders;

public interface IOrderService
{
    Task<OrderSummary> CreateOrderAsync(string userId, CheckoutRequest request, bool payNow, CancellationToken cancellationToken = default);
    Task<OrderSummary?> GetByNumberAsync(string orderNumber, string? userId = null, bool admin = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummary>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummary>> GetMineAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken = default);
    Task MarkPaidAsync(string orderNumber, string paymentReference, string? provider = null, CancellationToken cancellationToken = default);
    Task AttachPaymentSessionAsync(string orderNumber, string provider, string? reference, CancellationToken cancellationToken = default);
}
