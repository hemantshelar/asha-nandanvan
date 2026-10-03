namespace AshaNandanvan.Web.Endpoints;

public static class VisitEndpoints
{
    /// <summary>
    /// Short share links: /go/gt lands on the home page tagged as Gumtree, and
    /// /go/fb?to=/offer/dog-sitting sends Facebook straight to the booking page. Handy
    /// behind a QR code, and shorter to type into a classified than a utm_source string.
    /// </summary>
    public static IEndpointRouteBuilder MapVisitEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/go/{code}", (string code, string? to) =>
        {
            var target = string.IsNullOrWhiteSpace(to) || !to.StartsWith('/') ? "/" : to;
            var separator = target.Contains('?') ? "&" : "?";
            return Results.Redirect($"{target}{separator}s={Uri.EscapeDataString(code)}");
        });

        return endpoints;
    }
}
