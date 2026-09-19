using AshaNandanvan.Application.Orders;
using AshaNandanvan.Application.Payments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AshaNandanvan.Infrastructure.Payments;

/// <summary>
/// Used when Stripe keys are not configured so the pickup checkout can still be demonstrated.
/// </summary>
public sealed class MockPaymentProvider : IPaymentProvider
{
    private readonly IOrderService _orders;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MockPaymentProvider> _logger;

    public MockPaymentProvider(
        IOrderService orders,
        IHttpContextAccessor httpContextAccessor,
        ILogger<MockPaymentProvider> logger)
    {
        _orders = orders;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string Name => "Mock";

    public async Task<PaymentSessionResult> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var reference = $"mock_{Guid.NewGuid():N}";
        await _orders.MarkPaidAsync(request.OrderNumber, reference, cancellationToken);
        _logger.LogInformation("Mock payment completed for {OrderNumber}.", request.OrderNumber);

        var http = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HTTP context.");
        var url = $"{http.Request.Scheme}://{http.Request.Host}/checkout/confirmation/{request.OrderNumber}";
        return new PaymentSessionResult(url, reference);
    }

    public Task ConfirmReturnAsync(string orderNumber, string? sessionId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task HandleWebhookAsync(string payload, string? signature, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
