using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AshaNandanvan.Application.Common;
using AshaNandanvan.Application.Media;
using Microsoft.AspNetCore.Mvc;

namespace AshaNandanvan.Web.Endpoints;

public static class YouTubeEndpoints
{
    private const string StateCookie = "youtube_connect";
    private static readonly Regex RangeRegex = new(
        @"bytes (\d+)-(\d+)/(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static IEndpointRouteBuilder MapYouTubeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/admin/youtube/connect", (
            HttpContext context,
            IYouTubeUploadService youtube,
            string? returnUrl) =>
        {
            var redirectUri = CallbackUri(context);
            var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            var safeReturn = SafeReturn(returnUrl);
            context.Response.Cookies.Append(
                StateCookie,
                $"{state}|{safeReturn}",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromMinutes(15)
                });
            return Results.Redirect(youtube.GetAuthorizeUrl(redirectUri, state));
        }).RequireAuthorization(AppRoles.Admin);

        endpoints.MapGet("/admin/youtube/callback", async (
            HttpContext context,
            IYouTubeUploadService youtube,
            string? code,
            string? state,
            string? error) =>
        {
            var stored = context.Request.Cookies[StateCookie];
            context.Response.Cookies.Delete(StateCookie);
            var returnUrl = "/media";
            if (!string.IsNullOrWhiteSpace(stored))
            {
                var parts = stored.Split('|', 2);
                if (parts.Length == 2)
                {
                    returnUrl = SafeReturn(parts[1]);
                    if (!string.Equals(parts[0], state, StringComparison.Ordinal))
                    {
                        return Results.Redirect(WithQuery(returnUrl, "youtube", "denied"));
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(code))
            {
                return Results.Redirect(WithQuery(returnUrl, "youtube", "denied"));
            }

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Please sign in again.");
            try
            {
                await youtube.ConnectAsync(code, CallbackUri(context), userId);
                return Results.Redirect(WithQuery(returnUrl, "youtube", "connected"));
            }
            catch
            {
                return Results.Redirect(WithQuery(returnUrl, "youtube", "denied"));
            }
        }).RequireAuthorization(AppRoles.Admin);

        endpoints.MapPost("/admin/youtube/disconnect", async (IYouTubeUploadService youtube) =>
        {
            await youtube.DisconnectAsync();
            return Results.Redirect("/media");
        }).RequireAuthorization(AppRoles.Admin).DisableAntiforgery();

        endpoints.MapPut("/admin/youtube/sessions/{sessionId}", async (
            string sessionId,
            HttpRequest request,
            IYouTubeUploadService youtube,
            CancellationToken cancellationToken) =>
        {
            if (!request.Headers.TryGetValue("Content-Range", out var rangeValues)
                || !TryParseRange(rangeValues.ToString(), out var start, out var end, out var total))
            {
                return Results.BadRequest(new { message = "Missing Content-Range." });
            }

            try
            {
                var result = await youtube.UploadChunkAsync(
                    sessionId,
                    request.Body,
                    start,
                    end,
                    total,
                    cancellationToken);
                return Results.Json(new
                {
                    completed = result.Completed,
                    videoId = result.VideoId,
                    nextOffset = result.NextOffset
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .RequireAuthorization(AppRoles.Admin)
        .DisableAntiforgery()
        .WithMetadata(new RequestSizeLimitAttribute(12_000_000));

        return endpoints;
    }

    private static string CallbackUri(HttpContext context) =>
        $"{context.Request.Scheme}://{context.Request.Host}/admin/youtube/callback";

    private static string SafeReturn(string? returnUrl) =>
        string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/') || returnUrl.StartsWith("//")
            ? "/media"
            : returnUrl;

    private static string WithQuery(string path, string key, string value)
    {
        var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{path}{separator}{key}={Uri.EscapeDataString(value)}";
    }

    private static bool TryParseRange(string? value, out long start, out long end, out long total)
    {
        start = end = total = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = RangeRegex.Match(value);
        if (!match.Success)
        {
            return false;
        }

        start = long.Parse(match.Groups[1].Value);
        end = long.Parse(match.Groups[2].Value);
        total = long.Parse(match.Groups[3].Value);
        return end >= start && total > end;
    }
}
