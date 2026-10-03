using AshaNandanvan.Application.Analytics;
using AshaNandanvan.Application.Options;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Domain.Enums;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AshaNandanvan.Infrastructure.Analytics;

public sealed class VisitStore : IVisitStore
{
    // Guards against the same page being written twice when a render happens to repeat.
    private static readonly TimeSpan PageViewDebounce = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan EventDebounce = TimeSpan.FromMinutes(5);

    // A visit is written on its own context: a Blazor circuit keeps one scoped context alive
    // for its whole life, and tracking must never collide with whatever a page is doing.
    private readonly IDbContextFactory<AppDbContext> _contexts;
    private readonly AnalyticsOptions _options;

    public VisitStore(IDbContextFactory<AppDbContext> contexts, IOptions<AnalyticsOptions> options)
    {
        _contexts = contexts;
        _options = options.Value;
    }

    private TimeSpan SessionTimeout => TimeSpan.FromMinutes(Math.Max(5, _options.SessionTimeoutMinutes));

    public async Task<int> StartOrResumeAsync(VisitStamp stamp, CancellationToken cancellationToken = default)
    {
        await using var db = await _contexts.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var existing = await db.VisitSessions
            .FirstOrDefaultAsync(s => s.SessionKey == stamp.SessionKey, cancellationToken);

        if (existing is not null)
        {
            existing.LastSeenAt = now;

            // A visit that started as Direct can still earn a real source if the next
            // request in the same session arrives on a tagged link.
            if (existing.Source == VisitSignals.Direct && stamp.Source != VisitSignals.Direct)
            {
                existing.Source = stamp.Source;
                existing.Campaign ??= stamp.Campaign;
                existing.Medium ??= stamp.Medium;
            }

            if (!string.IsNullOrWhiteSpace(stamp.UserId))
            {
                existing.UserId = stamp.UserId;
            }

            await db.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var session = new VisitSession
        {
            VisitorId = stamp.VisitorId,
            SessionKey = stamp.SessionKey,
            StartedAt = now,
            LastSeenAt = now,
            PageViews = 0,
            IsFirstVisit = stamp.IsFirstVisit,
            UserId = stamp.UserId,
            Source = stamp.Source,
            FirstTouchSource = stamp.FirstTouchSource,
            Campaign = stamp.Campaign,
            Medium = stamp.Medium,
            Referrer = stamp.Referrer,
            LandingPath = stamp.LandingPath,
            IpHash = stamp.IpHash,
            IpNetwork = stamp.IpNetwork,
            Country = stamp.Country,
            Device = stamp.Agent.Device,
            Browser = Blank(stamp.Agent.Browser),
            BrowserVersion = Blank(stamp.Agent.BrowserVersion),
            Platform = Blank(stamp.Agent.Platform),
            PlatformVersion = Blank(stamp.Agent.PlatformVersion),
            DeviceModel = stamp.Agent.Model,
            UserAgent = stamp.UserAgent,
            IsBot = stamp.Agent.IsBot
        };

        db.VisitSessions.Add(session);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return session.Id;
        }
        catch (DbUpdateException)
        {
            // Two requests from the same new session can race for the insert; the loser
            // just reuses the row the winner created.
            db.Entry(session).State = EntityState.Detached;
            var raced = await db.VisitSessions
                .FirstOrDefaultAsync(s => s.SessionKey == stamp.SessionKey, cancellationToken);
            return raced?.Id ?? 0;
        }
    }

    public async Task<int?> FindBySessionKeyAsync(Guid sessionKey, CancellationToken cancellationToken = default)
    {
        await using var db = await _contexts.CreateDbContextAsync(cancellationToken);
        return await db.VisitSessions
            .AsNoTracking()
            .Where(s => s.SessionKey == sessionKey)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> RecordPageViewAsync(
        int sessionId,
        string path,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contexts.CreateDbContextAsync(cancellationToken);
        var session = await db.VisitSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return sessionId;
        }

        var now = DateTimeOffset.UtcNow;
        var cleanPath = VisitSignals.NormalisePath(path);

        if (now - session.LastSeenAt > SessionTimeout)
        {
            session = await RollOverAsync(db, session, cleanPath, now, cancellationToken);
        }

        var since = now - PageViewDebounce;
        var repeated = await db.VisitEvents.AnyAsync(
            e => e.VisitSessionId == session.Id
                && e.Kind == VisitEventKind.PageView
                && e.Path == cleanPath
                && e.At >= since,
            cancellationToken);

        if (!repeated)
        {
            db.VisitEvents.Add(new VisitEvent
            {
                VisitSessionId = session.Id,
                Kind = VisitEventKind.PageView,
                At = now,
                Path = cleanPath
            });
            session.PageViews++;
        }

        session.LastSeenAt = now;
        session.ExitPath = cleanPath;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            session.UserId = userId;
        }

        await db.SaveChangesAsync(cancellationToken);
        return session.Id;
    }

    public async Task RecordEventAsync(
        int sessionId,
        VisitEventKind kind,
        string path,
        string? detail = null,
        decimal? value = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contexts.CreateDbContextAsync(cancellationToken);
        var session = await db.VisitSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var cleanPath = VisitSignals.NormalisePath(path);
        var trimmed = Trim(detail, 200);
        var since = now - EventDebounce;

        var repeated = await db.VisitEvents.AnyAsync(
            e => e.VisitSessionId == session.Id
                && e.Kind == kind
                && e.Detail == trimmed
                && e.At >= since,
            cancellationToken);

        if (repeated)
        {
            return;
        }

        db.VisitEvents.Add(new VisitEvent
        {
            VisitSessionId = session.Id,
            Kind = kind,
            At = now,
            Path = cleanPath,
            Detail = trimmed,
            Value = value
        });

        session.LastSeenAt = now;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            session.UserId = userId;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetViewportAsync(int sessionId, int width, CancellationToken cancellationToken = default)
    {
        if (width <= 0 || width > 10000)
        {
            return;
        }

        await using var db = await _contexts.CreateDbContextAsync(cancellationToken);
        await db.VisitSessions
            .Where(s => s.Id == sessionId && s.ViewportWidth == null)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.ViewportWidth, width), cancellationToken);
    }

    public async Task<int> PurgeBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        await using var db = await _contexts.CreateDbContextAsync(cancellationToken);
        return await db.VisitSessions
            .Where(s => s.LastSeenAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task ForgetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _contexts.CreateDbContextAsync(cancellationToken);
        await db.VisitSessions
            .Where(s => s.UserId == userId)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.UserId, (string?)null), cancellationToken);
    }

    /// <summary>
    /// A tab left open overnight still holds a live circuit. Rather than stretching one
    /// visit across twelve hours, close it off and carry the device details into a new one.
    /// </summary>
    private static async Task<VisitSession> RollOverAsync(
        AppDbContext db,
        VisitSession previous,
        string landingPath,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var next = new VisitSession
        {
            VisitorId = previous.VisitorId,
            SessionKey = Guid.NewGuid(),
            StartedAt = now,
            LastSeenAt = now,
            PageViews = 0,
            IsFirstVisit = false,
            UserId = previous.UserId,
            Source = previous.Source,
            FirstTouchSource = previous.FirstTouchSource,
            Campaign = previous.Campaign,
            Medium = previous.Medium,
            Referrer = previous.Referrer,
            LandingPath = landingPath,
            IpHash = previous.IpHash,
            IpNetwork = previous.IpNetwork,
            Country = previous.Country,
            Device = previous.Device,
            Browser = previous.Browser,
            BrowserVersion = previous.BrowserVersion,
            Platform = previous.Platform,
            PlatformVersion = previous.PlatformVersion,
            DeviceModel = previous.DeviceModel,
            ViewportWidth = previous.ViewportWidth,
            UserAgent = previous.UserAgent,
            IsBot = previous.IsBot
        };

        db.VisitSessions.Add(next);
        await db.SaveChangesAsync(cancellationToken);
        return next;
    }

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

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
