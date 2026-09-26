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
    public static readonly OrderStatus[] FilterChoices =
    [
        OrderStatus.Placed,
        OrderStatus.Confirmed,
        OrderStatus.Rejected,
        OrderStatus.Paid
    ];

    public static OrderStatus VisibleStatus(this OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment or OrderStatus.Placed => OrderStatus.Placed,
        OrderStatus.Cancelled => OrderStatus.Rejected,
        OrderStatus.ReadyForPickup or OrderStatus.Completed => OrderStatus.Paid,
        _ => status
    };

    public static bool IsPending(this OrderStatus status) =>
        status.VisibleStatus() == OrderStatus.Placed;

    public static bool IsApproved(this OrderStatus status) =>
        status.VisibleStatus() == OrderStatus.Confirmed;

    public static bool IsRejected(this OrderStatus status) =>
        status.VisibleStatus() == OrderStatus.Rejected;

    public static bool IsPaid(this OrderStatus status) =>
        status.VisibleStatus() == OrderStatus.Paid;

    public static bool IsUnpaid(this OrderStatus status) => status.IsApproved();

    public static string DisplayName(this OrderStatus status) => status.VisibleStatus() switch
    {
        OrderStatus.Placed => "Pending",
        OrderStatus.Confirmed => "Approved",
        OrderStatus.Rejected => "Rejected",
        OrderStatus.Paid => "Paid",
        _ => status.ToString()
    };
}
