using AshaNandanvan.Application.Payments;

namespace AshaNandanvan.Web.Endpoints;

public static class WebhookEndpoints
{
    public static IEndpointRouteBuilder MapPaymentWebhooks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/webhooks/stripe", async (HttpRequest request, IPaymentProvider payment) =>
        {
            using var reader = new StreamReader(request.Body);
            var payload = await reader.ReadToEndAsync();
            var signature = request.Headers["Stripe-Signature"].ToString();
            await payment.HandleWebhookAsync(payload, signature);
            return Results.Ok();
        }).DisableAntiforgery();

        return endpoints;
    }
}
