using AshaNandanvan.Application.Options;
using AshaNandanvan.Application.Orders;
using AshaNandanvan.Application.Payments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace AshaNandanvan.Infrastructure.Payments;

public sealed class StripePaymentProvider : IPaymentProvider
{
    private readonly IOrderService _orders;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PaymentOptions _options;
    private readonly ILogger<StripePaymentProvider> _logger;

    public StripePaymentProvider(
        IOrderService orders,
        IHttpContextAccessor httpContextAccessor,
        IOptions<PaymentOptions> options,
        ILogger<StripePaymentProvider> logger)
    {
        _orders = orders;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        _logger = logger;
        if (!string.IsNullOrWhiteSpace(_options.Stripe.SecretKey))
        {
            StripeConfiguration.ApiKey = _options.Stripe.SecretKey;
        }
    }

    public string Name => "Stripe";

    public async Task<PaymentSessionResult> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var http = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HTTP context.");
        var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = request.CustomerEmail,
            ClientReferenceId = request.OrderNumber,
            SuccessUrl = $"{baseUrl}{_options.SuccessPath}/{request.OrderNumber}?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}{_options.CancelPath}",
            Metadata = new Dictionary<string, string> { ["orderNumber"] = request.OrderNumber },
            LineItems = request.Lines.Select(line => new SessionLineItemOptions
            {
                Quantity = line.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = request.Currency.ToLowerInvariant(),
                    UnitAmount = (long)Math.Round(line.UnitPrice * 100m, MidpointRounding.AwayFromZero),
                    ProductData = new SessionLineItemPriceDataProductDataOptions { Name = line.Name }
                }
            }).ToList()
        };

        var session = await new SessionService().CreateAsync(sessionOptions, cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException("Stripe did not return a checkout URL.");
        }

        return new PaymentSessionResult(session.Url, session.Id);
    }

    public async Task ConfirmReturnAsync(string orderNumber, string? sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        var session = await new SessionService().GetAsync(sessionId, cancellationToken: cancellationToken);
        if (session.PaymentStatus is "paid" or "no_payment_required" || session.Status == "complete")
        {
            await _orders.MarkPaidAsync(orderNumber, session.PaymentIntentId ?? session.Id, Name, cancellationToken);
        }
    }

    public async Task HandleWebhookAsync(string payload, string? signature, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Stripe.WebhookSecret))
        {
            _logger.LogWarning("Stripe webhook received but WebhookSecret is not configured.");
            return;
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, _options.Stripe.WebhookSecret);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature.");
            throw;
        }

        if (stripeEvent.Type is not "checkout.session.completed")
        {
            return;
        }

        if (stripeEvent.Data.Object is not Session session)
        {
            return;
        }

        var orderNumber = session.ClientReferenceId
            ?? session.Metadata.GetValueOrDefault("orderNumber");

        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            _logger.LogWarning("Stripe session {SessionId} had no order number.", session.Id);
            return;
        }

        await _orders.MarkPaidAsync(orderNumber, session.PaymentIntentId ?? session.Id, Name, cancellationToken);
        _logger.LogInformation("Stripe marked order {OrderNumber} as paid.", orderNumber);
    }
}
