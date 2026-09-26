namespace AshaNandanvan.Domain.Enums;

public enum OrderStatus
{
    PendingPayment = 1,
    Paid = 2,
    ReadyForPickup = 3,
    Completed = 4,
    Cancelled = 5,
    Placed = 6,
    Rejected = 7,
    Confirmed = 8
}

public static class OrderStatusExtensions
{
    public static bool IsUnpaid(this OrderStatus status) =>
        status is OrderStatus.Placed or OrderStatus.PendingPayment or OrderStatus.Confirmed;

    public static bool AwaitsDecision(this OrderStatus status) =>
        status is OrderStatus.Placed or OrderStatus.PendingPayment or OrderStatus.Paid;

    public static bool IsClosed(this OrderStatus status) =>
        status is OrderStatus.Rejected or OrderStatus.Cancelled;

    public static string DisplayName(this OrderStatus status) => status switch
    {
        OrderStatus.Placed => "Awaiting approval",
        OrderStatus.PendingPayment => "Payment started",
        OrderStatus.Paid => "Paid — awaiting approval",
        OrderStatus.Confirmed => "Confirmed",
        OrderStatus.ReadyForPickup => "Ready",
        OrderStatus.Completed => "Completed",
        OrderStatus.Cancelled => "Cancelled",
        OrderStatus.Rejected => "Rejected",
        _ => status.ToString()
    };
}
