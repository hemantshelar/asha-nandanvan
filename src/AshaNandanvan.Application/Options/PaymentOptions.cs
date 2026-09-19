namespace AshaNandanvan.Application.Options;

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    /// <summary>Stripe, Square (future), or Mock.</summary>
    public string Provider { get; set; } = "Stripe";

    public string SuccessPath { get; set; } = "/checkout/confirmation";
    public string CancelPath { get; set; } = "/checkout?cancelled=1";

    public StripeOptions Stripe { get; set; } = new();
}

public sealed class StripeOptions
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SecretKey);
}
