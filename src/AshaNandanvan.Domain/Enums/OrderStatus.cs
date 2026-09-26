namespace AshaNandanvan.Domain.Enums;

public enum OrderStatus
{
    PendingPayment = 1,
    Paid = 2,
    ReadyForPickup = 3,
    Completed = 4,
    Cancelled = 5,
    Placed = 6
}

public static class OrderStatusExtensions
{
    public static bool IsUnpaid(this OrderStatus status) =>
        status is OrderStatus.Placed or OrderStatus.PendingPayment;

    public static string DisplayName(this OrderStatus status) => status switch
    {
        OrderStatus.Placed => "Placed — pay later",
        OrderStatus.PendingPayment => "Payment started",
        OrderStatus.Paid => "Paid",
        OrderStatus.ReadyForPickup => "Ready",
        OrderStatus.Completed => "Completed",
        OrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };
}
