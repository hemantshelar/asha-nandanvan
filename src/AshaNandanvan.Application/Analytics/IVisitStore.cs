using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Application.Analytics;

/// <summary>Everything the first request of a visit tells us about it.</summary>
public sealed record VisitStamp(
    Guid VisitorId,
    Guid SessionKey,
    bool IsFirstVisit,
    string Source,
    string FirstTouchSource,
    string? Campaign,
    string? Medium,
    string? Referrer,
    string LandingPath,
    string IpHash,
    string? IpNetwork,
    string? Country,
    VisitAgent Agent,
    string? UserAgent,
    string? UserId);

public interface IVisitStore
{
    /// <summary>Creates the visit row, or extends it when the same session comes back.</summary>
    Task<int> StartOrResumeAsync(VisitStamp stamp, CancellationToken cancellationToken = default);

    Task<int?> FindBySessionKeyAsync(Guid sessionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a page view. Returns the visit to keep writing to, which differs from the one
    /// passed in when a long-idle tab comes back to life and the old visit has to be closed off.
    /// </summary>
    Task<int> RecordPageViewAsync(
        int sessionId,
        string path,
        string? userId,
        CancellationToken cancellationToken = default);

    Task RecordEventAsync(
        int sessionId,
        VisitEventKind kind,
        string path,
        string? detail = null,
        decimal? value = null,
        string? userId = null,
        CancellationToken cancellationToken = default);

    Task SetViewportAsync(int sessionId, int width, CancellationToken cancellationToken = default);

    Task<int> PurgeBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);

    /// <summary>Unlinks a deleted account from its visits, leaving the counts intact.</summary>
    Task ForgetUserAsync(string userId, CancellationToken cancellationToken = default);
}

public interface IVisitTracker
{
    /// <summary>Attaches the live circuit to the visit the request pipeline already opened.</summary>
    Task BeginAsync(Guid sessionKey, int? viewportWidth, CancellationToken cancellationToken = default);

    Task PageViewAsync(string path, CancellationToken cancellationToken = default);

    Task EventAsync(
        VisitEventKind kind,
        string? detail = null,
        decimal? value = null,
        CancellationToken cancellationToken = default);
}
