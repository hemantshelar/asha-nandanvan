using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AshaNandanvan.Application.Media;
using AshaNandanvan.Application.Options;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AshaNandanvan.Infrastructure.Media;

public sealed class YouTubeUploadService : IYouTubeUploadService
{
    public const string HttpClientName = "YouTube";
    private const string ScopeUpload = "https://www.googleapis.com/auth/youtube.upload";
    private const string ScopeReadonly = "https://www.googleapis.com/auth/youtube.readonly";

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptions<GoogleAuthOptions> _google;
    private readonly YouTubeUploadSessionStore _sessions;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _accessExpires;

    public YouTubeUploadService(
        IDbContextFactory<AppDbContext> dbFactory,
        IHttpClientFactory httpFactory,
        IOptions<GoogleAuthOptions> google,
        YouTubeUploadSessionStore sessions)
    {
        _dbFactory = dbFactory;
        _httpFactory = httpFactory;
        _google = google;
        _sessions = sessions;
    }

    public async Task<YouTubeConnectionStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var configured = _google.Value.IsConfigured;
        if (!configured)
        {
            return new YouTubeConnectionStatus(false, false, string.Empty);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var link = await db.YouTubeChannelLinks.AsNoTracking().OrderBy(l => l.Id).FirstOrDefaultAsync(cancellationToken);
        return link is null
            ? new YouTubeConnectionStatus(true, false, string.Empty)
            : new YouTubeConnectionStatus(true, true, link.ChannelTitle);
    }

    public string GetAuthorizeUrl(string redirectUri, string state)
    {
        var google = RequireGoogle();
        var query = new Dictionary<string, string>
        {
            ["client_id"] = google.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = $"{ScopeUpload} {ScopeReadonly}",
            ["access_type"] = "offline",
            ["prompt"] = "consent",
            ["include_granted_scopes"] = "true",
            ["state"] = state
        };
        return "https://accounts.google.com/o/oauth2/v2/auth?" +
               string.Join('&', query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
    }

    public async Task ConnectAsync(string code, string redirectUri, string adminUserId, CancellationToken cancellationToken = default)
    {
        var google = RequireGoogle();
        var http = _httpFactory.CreateClient(HttpClientName);
        using var tokenResponse = await http.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = google.ClientId,
                ["client_secret"] = google.ClientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            }),
            cancellationToken);
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GoogleError(tokenJson, "Google did not finish connecting YouTube."));
        }

        var tokens = JsonSerializer.Deserialize<TokenResponse>(tokenJson, Json)
            ?? throw new InvalidOperationException("Google did not return tokens.");
        if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
        {
            throw new InvalidOperationException("Google did not return a refresh token. Disconnect the app from your Google account and try again.");
        }

        CacheAccess(tokens);
        var channel = await FetchChannelAsync(tokens.AccessToken, cancellationToken);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var link = await db.YouTubeChannelLinks.OrderBy(l => l.Id).FirstOrDefaultAsync(cancellationToken);
        if (link is null)
        {
            link = new YouTubeChannelLink();
            db.YouTubeChannelLinks.Add(link);
        }

        link.RefreshToken = tokens.RefreshToken;
        link.ChannelId = channel.Id;
        link.ChannelTitle = channel.Title;
        link.ConnectedByUserId = adminUserId;
        link.ConnectedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await db.YouTubeChannelLinks.ExecuteDeleteAsync(cancellationToken);
        _accessToken = null;
        _accessExpires = DateTimeOffset.MinValue;
    }

    public async Task<YouTubeUploadSession> StartResumableAsync(YouTubeUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length is < 2 or > 100)
        {
            throw new InvalidOperationException("Give the video a short title.");
        }

        if (request.ContentLength is <= 0 or > 2L * 1024 * 1024 * 1024)
        {
            throw new InvalidOperationException("Choose a video under 2 GB.");
        }

        var access = await GetAccessTokenAsync(cancellationToken);
        var privacy = request.Privacy == YouTubePrivacy.Public ? "public" : "unlisted";
        var body = JsonSerializer.Serialize(new
        {
            snippet = new
            {
                title = request.Title.Trim(),
                description = request.Description.Trim(),
                tags = request.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Take(10).ToArray(),
                categoryId = "22"
            },
            status = new
            {
                privacyStatus = privacy,
                selfDeclaredMadeForKids = false
            }
        });

        var http = _httpFactory.CreateClient(HttpClientName);
        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            "https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&part=snippet,status")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
        message.Headers.TryAddWithoutValidation("X-Upload-Content-Length", request.ContentLength.ToString());
        message.Headers.TryAddWithoutValidation(
            "X-Upload-Content-Type",
            string.IsNullOrWhiteSpace(request.ContentType) ? "video/mp4" : request.ContentType);

        using var response = await http.SendAsync(message, cancellationToken);
        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GoogleError(error, "YouTube did not start the upload."));
        }

        if (!response.Headers.TryGetValues("Location", out var locations)
            && !response.Headers.TryGetValues("location", out locations))
        {
            throw new InvalidOperationException("YouTube did not return an upload URL.");
        }

        var sessionId = Guid.NewGuid().ToString("N");
        _sessions.Add(sessionId, locations.First(), request.ContentLength);
        return new YouTubeUploadSession(sessionId, request.ContentLength);
    }

    public async Task<YouTubeChunkResult> UploadChunkAsync(
        string sessionId,
        Stream chunk,
        long start,
        long endInclusive,
        long total,
        CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryGet(sessionId, out var session))
        {
            throw new InvalidOperationException("That upload session expired. Start again.");
        }

        if (total != session.ContentLength)
        {
            throw new InvalidOperationException("The file size changed. Start the upload again.");
        }

        using var content = new StreamContent(chunk);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Headers.ContentRange = new ContentRangeHeaderValue(start, endInclusive, total);

        var http = _httpFactory.CreateClient(HttpClientName);
        using var message = new HttpRequestMessage(HttpMethod.Put, session.UploadUri) { Content = content };
        using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if ((int)response.StatusCode == 308)
        {
            return new YouTubeChunkResult(false, null, endInclusive + 1);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GoogleError(text, "YouTube rejected this part of the upload."));
        }

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        var videoId = doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new InvalidOperationException("YouTube finished the upload but did not return a video id.");
        }

        _sessions.Remove(sessionId);
        return new YouTubeChunkResult(true, videoId, total);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_accessToken) && _accessExpires > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return _accessToken;
            }

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var link = await db.YouTubeChannelLinks.AsNoTracking().OrderBy(l => l.Id).FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("Connect YouTube first.");

            var google = RequireGoogle();
            var http = _httpFactory.CreateClient(HttpClientName);
            using var response = await http.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["refresh_token"] = link.RefreshToken,
                    ["client_id"] = google.ClientId,
                    ["client_secret"] = google.ClientSecret,
                    ["grant_type"] = "refresh_token"
                }),
                cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(GoogleError(json, "Reconnect YouTube — the saved login is no longer valid."));
            }

            var tokens = JsonSerializer.Deserialize<TokenResponse>(json, Json)
                ?? throw new InvalidOperationException("Google did not return an access token.");
            CacheAccess(tokens);
            return _accessToken!;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<(string Id, string Title)> FetchChannelAsync(string accessToken, CancellationToken cancellationToken)
    {
        var http = _httpFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GoogleError(json, "Could not read the YouTube channel."));
        }

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("That Google account has no YouTube channel yet. Create one, then connect again.");
        }

        var first = items[0];
        var id = first.GetProperty("id").GetString() ?? string.Empty;
        var title = first.GetProperty("snippet").GetProperty("title").GetString() ?? "YouTube";
        return (id, title);
    }

    private void CacheAccess(TokenResponse tokens)
    {
        if (!string.IsNullOrWhiteSpace(tokens.AccessToken))
        {
            _accessToken = tokens.AccessToken;
            var seconds = tokens.ExpiresIn is > 0 ? tokens.ExpiresIn : 3500;
            _accessExpires = DateTimeOffset.UtcNow.AddSeconds(seconds);
        }
    }

    private GoogleAuthOptions RequireGoogle() =>
        _google.Value.IsConfigured
            ? _google.Value
            : throw new InvalidOperationException("Google sign-in keys are not configured on this server.");

    private static string GoogleError(string json, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error_description", out var description)
                && description.GetString() is { Length: > 0 } text)
            {
                return text;
            }

            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object
                    && error.TryGetProperty("message", out var message)
                    && message.GetString() is { Length: > 0 } fromObject)
                {
                    return fromObject;
                }

                if (error.GetString() is { Length: > 0 } fromString)
                {
                    return fromString;
                }
            }
        }
        catch (JsonException)
        {
        }

        return fallback;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
