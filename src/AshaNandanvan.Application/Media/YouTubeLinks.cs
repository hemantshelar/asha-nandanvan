using System.Text.RegularExpressions;

namespace AshaNandanvan.Application.Media;

public static partial class YouTubeLinks
{
    public static bool TryParseVideoId(string? url, out string videoId)
    {
        videoId = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        var match = VideoIdRegex().Match(trimmed);
        if (!match.Success)
        {
            return false;
        }

        videoId = match.Groups[1].Value;
        return true;
    }

    public static string ThumbnailUrl(string videoId) =>
        $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";

    public static string EmbedUrl(string videoId) =>
        $"https://www.youtube-nocookie.com/embed/{videoId}?rel=0";

    public static string WatchUrl(string videoId) =>
        $"https://www.youtube.com/watch?v={videoId}";

    [GeneratedRegex(
        @"(?:youtube\.com/(?:watch\?(?:.*&)?v=|embed/|shorts/|live/)|youtu\.be/)([A-Za-z0-9_-]{11})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VideoIdRegex();
}
