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
    public bool CanPay =>
        Status.IsUnpaid()
        && string.IsNullOrWhiteSpace(PaymentReference)
        && !HasTrialStay;

    public bool HasTrialStay => Items.Any(i => i.IsTrialStay);

    public bool TrialStayCompleted =>
        HasTrialStay
        && (Status == OrderStatus.Completed
            || Items.Any(i => i.IsTrialStay && i.StayEndsAt is DateTimeOffset end && end <= DateTimeOffset.UtcNow));

    public bool CanAcceptOriginalStay =>
        TrialStayCompleted
        && !Status.IsClosed()
        && Items.Any(i => i.IsTrialStay && i.IntendedStayStartsAt is not null && i.IntendedStayEndsAt is not null);

    public bool CanApprove =>
        !HasTrialStay && Status.AwaitsDecision();

    public bool CanReject =>
        HasTrialStay
            ? TrialStayCompleted && !Status.IsClosed()
            : Status.AwaitsDecision() || Status == OrderStatus.Confirmed;
}

public sealed record OrderLineSummary(
    string ProductName,
    string Unit,
    int Quantity,
    decimal UnitPrice,
    string? SlotLabel = null,
    bool IsTrialStay = false,
    DateTimeOffset? StayEndsAt = null,
    DateTimeOffset? IntendedStayStartsAt = null,
    DateTimeOffset? IntendedStayEndsAt = null);
