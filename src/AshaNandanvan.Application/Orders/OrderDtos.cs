using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Application.Orders;

public sealed record CheckoutRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly PickupDate { get; set; }
    public string PickupWindow { get; set; } = string.Empty;
}

public sealed record OrderSummary(
    int Id,
    string OrderNumber,
    OrderStatus Status,
    decimal Total,
    DateOnly PickupDate,
    string PickupWindow,
    string CustomerName,
    string CustomerEmail,
    string? Phone,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderLineSummary> Items,
    string? PaymentReference = null,
    string PaymentProvider = "")
{
    public bool CanPay => Status.IsUnpaid();
}

public sealed record OrderLineSummary(
    string ProductName,
    string Unit,
    int Quantity,
    decimal UnitPrice,
    string? SlotLabel = null);
