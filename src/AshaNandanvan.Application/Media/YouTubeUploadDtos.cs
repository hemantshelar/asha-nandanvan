namespace AshaNandanvan.Application.Media;

public enum YouTubePrivacy
{
    Unlisted = 1,
    Public = 2
}

public sealed record YouTubeConnectionStatus(
    bool IsConfigured,
    bool IsConnected,
    string ChannelTitle);

public sealed record YouTubeUploadRequest(
    string Title,
    string Description,
    IReadOnlyList<string> Tags,
    YouTubePrivacy Privacy,
    string ContentType,
    long ContentLength);

public sealed record YouTubeUploadSession(string SessionId, long ContentLength);

public sealed record YouTubeChunkResult(bool Completed, string? VideoId, long NextOffset);

public sealed class PickedVideoFile
{
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Type { get; set; } = string.Empty;
}
