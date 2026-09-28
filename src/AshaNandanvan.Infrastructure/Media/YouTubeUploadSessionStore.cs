using System.Collections.Concurrent;

namespace AshaNandanvan.Infrastructure.Media;

public sealed class YouTubeUploadSessionStore
{
    private readonly ConcurrentDictionary<string, PendingUpload> _sessions = new();

    public void Add(string id, string uploadUri, long contentLength) =>
        _sessions[id] = new PendingUpload(uploadUri, contentLength, DateTimeOffset.UtcNow.AddHours(6));

    public bool TryGet(string id, out PendingUpload session)
    {
        if (_sessions.TryGetValue(id, out session!) && session.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return true;
        }

        _sessions.TryRemove(id, out _);
        session = null!;
        return false;
    }

    public void Remove(string id) => _sessions.TryRemove(id, out _);

    public sealed record PendingUpload(string UploadUri, long ContentLength, DateTimeOffset ExpiresAt);
}
