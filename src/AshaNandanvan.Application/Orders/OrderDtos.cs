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
        Status.IsApproved()
        && Total > 0
        && string.IsNullOrWhiteSpace(PaymentReference)
        && !HasTrialStay;

    public bool HasTrialStay => Items.Any(i => i.IsTrialStay);

    public bool CanApprove => Status.IsPending() || Status.IsRejected();

    public bool CanReject =>
        Status.IsPending() || (Status.IsApproved() && !HasTrialStay);

    public bool CanDeclineLongerStay =>
        HasTrialStay && Status.IsApproved();

    public bool CanPlaceOriginalStay =>
        HasTrialStay
        && Status.IsApproved()
        && Items.Any(i => i.IsTrialStay && i.IntendedStayStartsAt is not null && i.IntendedStayEndsAt is not null);

    public string StatusLabel =>
        HasTrialStay && Status.IsApproved() ? "Trial approved"
        : HasTrialStay && Status.IsPending() ? "Trial pending"
        : HasTrialStay && Status.IsRejected() ? "Not continuing"
        : Status.DisplayName();

    public string? CustomerNote =>
        HasTrialStay && Status.IsApproved()
            ? "Your trial night is approved. Once it is complete, we will decide whether we can take the longer stay, and we will write to you either way. There is no obligation on either side."
            : HasTrialStay && Status.IsPending()
                ? "We have your trial request and will confirm it shortly."
                : HasTrialStay && Status.IsRejected()
                    ? "Thank you for the trial visit. We will not proceed with the longer stay this time."
                    : Status.IsApproved()
                        ? "This stay is approved. You can pay online from My orders, or in cash at the backyard."
                        : null;
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
