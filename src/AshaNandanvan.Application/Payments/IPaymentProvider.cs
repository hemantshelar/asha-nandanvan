namespace AshaNandanvan.Application.Payments;

public sealed record PaymentRequest(
    string OrderNumber,
    string CustomerEmail,
    string Description,
    decimal Amount,
    string Currency,
    IReadOnlyList<PaymentLine> Lines);

public sealed record PaymentLine(string Name, int Quantity, decimal UnitPrice);

public sealed record PaymentSessionResult(string RedirectUrl, string? ProviderReference);

public interface IPaymentProvider
{
    string Name { get; }
    Task<PaymentSessionResult> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    Task ConfirmReturnAsync(string orderNumber, string? sessionId, CancellationToken cancellationToken = default);
    Task HandleWebhookAsync(string payload, string? signature, CancellationToken cancellationToken = default);
}
