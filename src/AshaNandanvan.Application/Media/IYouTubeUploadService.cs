namespace AshaNandanvan.Application.Media;

public interface IYouTubeUploadService
{
    Task<YouTubeConnectionStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    string GetAuthorizeUrl(string redirectUri, string state);
    Task ConnectAsync(string code, string redirectUri, string adminUserId, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<YouTubeUploadSession> StartResumableAsync(YouTubeUploadRequest request, CancellationToken cancellationToken = default);
    Task<YouTubeChunkResult> UploadChunkAsync(
        string sessionId,
        Stream chunk,
        long start,
        long endInclusive,
        long total,
        CancellationToken cancellationToken = default);
}
