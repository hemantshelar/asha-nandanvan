using System.Text.Json;
using AshaNandanvan.Application.Orders;
using AshaNandanvan.Application.Payments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Square;
using Square.Checkout.PaymentLinks;
using Square.Orders;
using PaymentOptions = AshaNandanvan.Application.Options.PaymentOptions;

namespace AshaNandanvan.Infrastructure.Payments;

public sealed class SquarePaymentProvider : IPaymentProvider
{
    private readonly IOrderService _orders;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PaymentOptions _options;
    private readonly ILogger<SquarePaymentProvider> _logger;

    public SquarePaymentProvider(
        IOrderService orders,
        IHttpContextAccessor httpContextAccessor,
        IOptions<PaymentOptions> options,
        ILogger<SquarePaymentProvider> logger)
    {
        _orders = orders;
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => _options.Square.UseSandbox ? "Square (sandbox)" : "Square";

    public async Task<PaymentSessionResult> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var square = _options.Square;
        if (!square.IsConfigured)
        {
            throw new InvalidOperationException("Square is not configured. Set AccessToken and LocationId in secrets.");
        }

        var http = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HTTP context.");
        var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
        var client = CreateClient();

        CreatePaymentLinkResponse response;
        try
        {
            response = await client.Checkout.PaymentLinks.CreateAsync(
                new CreatePaymentLinkRequest
                {
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    Description = request.OrderNumber,
                    PaymentNote = request.OrderNumber,
                    QuickPay = new QuickPay
                    {
                        Name = request.Description,
                        PriceMoney = new Money
                        {
                            Amount = (long)Math.Round(request.Amount * 100m, MidpointRounding.AwayFromZero),
                            Currency = ToCurrency(request.Currency)
                        },
                        LocationId = square.LocationId
                    },
                    CheckoutOptions = new CheckoutOptions
                    {
                        RedirectUrl = $"{baseUrl}{_options.SuccessPath}/{request.OrderNumber}",
                        AskForShippingAddress = false
                    },
                    PrePopulatedData = new PrePopulatedData
                    {
                        BuyerEmail = request.CustomerEmail
                    }
                },
                cancellationToken: cancellationToken);
        }
        catch (SquareApiException ex)
        {
            throw new InvalidOperationException(
                FormatErrors("Square could not create a sandbox checkout.", ex.Errors),
                ex);
        }

        var link = response.PaymentLink
            ?? throw new InvalidOperationException(FormatErrors("Square did not return a payment link.", response.Errors));

        if (string.IsNullOrWhiteSpace(link.Url))
        {
            throw new InvalidOperationException("Square payment link has no URL.");
        }

        await _orders.AttachPaymentSessionAsync(request.OrderNumber, Name, link.OrderId ?? link.Id, cancellationToken);
        return new PaymentSessionResult(link.Url, link.OrderId ?? link.Id);
    }

    public async Task ConfirmReturnAsync(string orderNumber, string? sessionId, CancellationToken cancellationToken = default)
    {
        if (!_options.Square.IsConfigured)
        {
            return;
        }

        var client = CreateClient();
        var existing = await _orders.GetByNumberAsync(orderNumber, admin: true, cancellationToken: cancellationToken);
        var squareOrderId = FirstNonEmpty(sessionId, existing?.PaymentReference);

        if (string.IsNullOrWhiteSpace(squareOrderId))
        {
            _logger.LogInformation("Square return for {OrderNumber} had no Square order id yet.", orderNumber);
            return;
        }

        try
        {
            var orderResponse = await client.Orders.GetAsync(
                new GetOrdersRequest { OrderId = squareOrderId },
                cancellationToken: cancellationToken);

            if (IsPaid(orderResponse.Order))
            {
                await _orders.MarkPaidAsync(orderNumber, squareOrderId, cancellationToken);
                _logger.LogInformation("Square marked order {OrderNumber} as paid.", orderNumber);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Square could not confirm return for {OrderNumber}.", orderNumber);
        }
    }

    public async Task<string> VerifyConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var square = _options.Square;
        if (!square.IsConfigured)
        {
            return "Square keys are missing. Set Payment:Square:AccessToken and Payment:Square:LocationId in user secrets or secrets.json.";
        }

        try
        {
            var client = CreateClient();
            var response = await client.Locations.ListAsync(cancellationToken: cancellationToken);
            var match = response.Locations?.Any(location => location.Id == square.LocationId) == true;
            var env = square.UseSandbox ? "sandbox" : "production";
            return match
                ? $"Square {env} connected and LocationId is valid."
                : $"Square {env} accepted the access token, but LocationId does not match any location on that account.";
        }
        catch (SquareApiException ex)
        {
            return FormatErrors("Square rejected the sandbox credentials.", ex.Errors);
        }
    }

    public async Task HandleWebhookAsync(string payload, string? signature, CancellationToken cancellationToken = default)
    {
        var square = _options.Square;
        if (!string.IsNullOrWhiteSpace(square.WebhookSignatureKey))
        {
            var http = _httpContextAccessor.HttpContext;
            var notificationUrl = http is null
                ? string.Empty
                : $"{http.Request.Scheme}://{http.Request.Host}{http.Request.Path}";

            if (!WebhooksHelper.VerifySignature(payload, signature ?? string.Empty, square.WebhookSignatureKey, notificationUrl))
            {
                throw new InvalidOperationException("Invalid Square webhook signature.");
            }
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;

        string? orderNumber = null;
        string? reference = null;

        if (root.TryGetProperty("data", out var data) && data.TryGetProperty("object", out var obj))
        {
            if (obj.TryGetProperty("payment", out var payment))
            {
                orderNumber = ReadString(payment, "note") ?? ReadString(payment, "reference_id");
                reference = ReadString(payment, "id");
            }
            else if (obj.TryGetProperty("order_updated", out var updated))
            {
                reference = ReadString(updated, "order_id");
            }
        }

        if (string.IsNullOrWhiteSpace(orderNumber) && !string.IsNullOrWhiteSpace(reference))
        {
            var client = CreateClient();
            var orderResponse = await client.Orders.GetAsync(
                new GetOrdersRequest { OrderId = reference },
                cancellationToken: cancellationToken);
            orderNumber = orderResponse.Order?.ReferenceId ?? orderResponse.Order?.TicketName;
            if (IsPaid(orderResponse.Order) && !string.IsNullOrWhiteSpace(orderNumber))
            {
                await _orders.MarkPaidAsync(orderNumber, reference, cancellationToken);
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(orderNumber))
        {
            await _orders.MarkPaidAsync(orderNumber, reference ?? type ?? "square", cancellationToken);
            _logger.LogInformation("Square webhook marked {OrderNumber} as paid ({Type}).", orderNumber, type);
        }
    }

    private SquareClient CreateClient()
    {
        var square = _options.Square;
        return new SquareClient(
            square.AccessToken,
            new ClientOptions
            {
                BaseUrl = square.UseSandbox ? SquareEnvironment.Sandbox : SquareEnvironment.Production
            });
    }

    private static bool IsPaid(Square.Order? order) =>
        order is not null
        && (order.State == OrderState.Completed
            || (order.Tenders is not null && order.Tenders.Any() && order.State != OrderState.Canceled));

    private static Currency ToCurrency(string code) =>
        code.Equals("AUD", StringComparison.OrdinalIgnoreCase) ? Currency.Aud : Currency.Usd;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string FormatErrors(string message, IEnumerable<Error>? errors)
    {
        if (errors is null)
        {
            return message;
        }

        var detail = string.Join("; ", errors.Select(e => e.Detail ?? e.Code.ToString() ?? "Square error"));
        return string.IsNullOrWhiteSpace(detail) ? message : $"{message} {detail}";
    }
}
