using System.Security.Claims;
using AshaNandanvan.Application.Analytics;
using AshaNandanvan.Application.Options;
using Microsoft.Extensions.Options;

namespace AshaNandanvan.Web.Services;

/// <summary>
/// Opens a visit on the first request and records the landing page view. Later page views
/// inside the same Blazor circuit never reach this pipeline, so <see cref="VisitTracker"/>
/// picks those up over the websocket instead.
/// </summary>
public sealed class VisitTrackingMiddleware
{
    public const string SessionIdKey = "asha.visit.sessionId";

    private static readonly string[] IgnoredPrefixes =
    [
        "/_", "/go/", "/css/", "/js/", "/lib/", "/images/", "/media/files/",
        "/payments/", "/admin/youtube/", "/signin-", "/health", "/favicon",
        "/apple-touch-icon", "/robots.txt", "/sitemap"
    ];

    private readonly RequestDelegate _next;

    public VisitTrackingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IVisitStore store,
        IOptions<AnalyticsOptions> options,
        ILogger<VisitTrackingMiddleware> logger)
    {
        var analytics = options.Value;

        // A 404 re-executes the pipeline against /not-found on the same HttpContext. The
        // second pass is skipped so the broken link is counted once, under the path shared.
        if (!analytics.Enabled || context.Items.ContainsKey(SessionIdKey) || !ShouldTrack(context))
        {
            await _next(context);
            return;
        }

        // Chromium only sends platform version and device model when we ask for them.
        context.Response.Headers["Accept-CH"] = "Sec-CH-UA-Platform-Version, Sec-CH-UA-Model";

        int? sessionId = null;
        try
        {
            sessionId = await OpenVisitAsync(context, store, analytics);
            if (sessionId is int id)
            {
                context.Items[SessionIdKey] = id;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not open a visit record for {Path}.", context.Request.Path);
        }

        await _next(context);

        if (sessionId is not int session || !IsPageResponse(context))
        {
            return;
        }

        try
        {
            // Deliberately not using the request token: a visitor who navigates away mid-load
            // still visited the page.
            await store.RecordPageViewAsync(session, context.Request.Path, UserId(context), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not record a page view for {Path}.", context.Request.Path);
        }
    }

    private static async Task<int> OpenVisitAsync(HttpContext context, IVisitStore store, AnalyticsOptions analytics)
    {
        var existingVisitor = VisitCookies.ReadGuid(context, VisitCookies.Visitor);
        var visitorId = existingVisitor ?? Guid.NewGuid();
        var sessionKey = VisitCookies.ReadGuid(context, VisitCookies.Session) ?? Guid.NewGuid();

        var source = ReadSource(context);
        var firstTouch = VisitCookies.ReadText(context, VisitCookies.FirstTouch) ?? source;

        var (hash, network) = VisitCookies.HashAddress(context.Connection.RemoteIpAddress, analytics.IpHashSalt);
        var userAgent = Header(context, "User-Agent");

        var agent = VisitSignals.Describe(userAgent, new ClientHints(
            Header(context, "Sec-CH-UA-Platform"),
            Header(context, "Sec-CH-UA-Platform-Version"),
            Header(context, "Sec-CH-UA-Model"),
            Header(context, "Sec-CH-UA-Mobile") == "?1"));

        var stamp = new VisitStamp(
            visitorId,
            sessionKey,
            existingVisitor is null,
            source,
            firstTouch,
            Trim(context.Request.Query["utm_campaign"], 120),
            Trim(context.Request.Query["utm_medium"], 40),
            Trim(Header(context, "Referer"), 500),
            VisitSignals.NormalisePath(context.Request.Path),
            hash,
            network,
            Trim(Header(context, "CF-IPCountry") ?? Header(context, "X-Country"), 8),
            agent,
            Trim(userAgent, 500),
            UserId(context));

        VisitCookies.WriteVisitor(context, visitorId);
        VisitCookies.WriteSession(context, sessionKey, TimeSpan.FromMinutes(Math.Max(5, analytics.SessionTimeoutMinutes)));
        VisitCookies.WriteFirstTouch(context, firstTouch);

        return await store.StartOrResumeAsync(stamp, context.RequestAborted);
    }

    /// <summary>
    /// A tag we put on a link we shared wins over the referrer, because social apps and
    /// classifieds routinely strip or rewrite the referrer on the way through.
    /// </summary>
    private static string ReadSource(HttpContext context)
    {
        var tagged = VisitSignals.FromTag(context.Request.Query["s"])
            ?? VisitSignals.FromTag(context.Request.Query["utm_source"])
            ?? VisitSignals.FromTag(context.Request.Query["ref"]);

        return tagged ?? VisitSignals.FromReferrer(Header(context, "Referer"), context.Request.Host.Host);
    }

    private static bool ShouldTrack(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
        foreach (var prefix in IgnoredPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Static files carry an extension; real pages on this site never do.
        if (Path.HasExtension(path))
        {
            return false;
        }

        return Header(context, "Accept")?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Only count a page that actually rendered. 404 counts too, because a dead link somebody
    /// shared is exactly the kind of thing worth seeing.
    /// </summary>
    private static bool IsPageResponse(HttpContext context)
    {
        // A 404 has no body yet at this point: the status-code middleware sits outside this
        // one and only re-executes /not-found after we unwind, so judge it on status alone.
        if (context.Response.StatusCode == 404)
        {
            return true;
        }

        return context.Response.StatusCode == 200
            && context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string? UserId(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;

    private static string? Header(HttpContext context, string name) =>
        context.Request.Headers.TryGetValue(name, out var values) && values.Count > 0
            ? values[0]
            : null;

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
