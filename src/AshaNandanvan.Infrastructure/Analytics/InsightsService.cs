using AshaNandanvan.Application.Analytics;
using AshaNandanvan.Application.Offers;
using AshaNandanvan.Domain.Enums;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Infrastructure.Analytics;

/// <summary>
/// Reads the visit tables into one report. Visit-level rows are pulled into memory because
/// a backyard shop measures traffic in thousands per quarter, not millions; the two tables
/// that can grow large (page views) are aggregated in SQL instead.
/// </summary>
public sealed class InsightsService : IInsightsService
{
    private const int RecentVisitLimit = 60;
    private const int TopPageLimit = 20;

    private static readonly OrderStatus[] PaidStatuses =
    [
        OrderStatus.Paid,
        OrderStatus.ReadyForPickup,
        OrderStatus.Completed
    ];

    private readonly AppDbContext _db;

    public InsightsService(AppDbContext db) => _db = db;

    public async Task<InsightsReport> GetAsync(InsightsQuery query, CancellationToken cancellationToken = default)
    {
        var days = Math.Clamp(query.Days, 1, 365);
        var now = DateTimeOffset.UtcNow;
        var offset = BookingPricing.SydneyOffset(now);
        var today = DateOnly.FromDateTime(BookingPricing.ToSydney(now).DateTime);
        var from = today.AddDays(-(days - 1));
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), offset);
        var end = new DateTimeOffset(today.AddDays(1).ToDateTime(TimeOnly.MinValue), offset);

        var sessions = await LoadSessionsAsync(start, end, query.IncludeBots, cancellationToken);
        var botVisits = await _db.VisitSessions
            .AsNoTracking()
            .CountAsync(s => s.IsBot && s.StartedAt >= start && s.StartedAt < end, cancellationToken);

        if (sessions.Count == 0)
        {
            var empty = InsightsReport.Empty(from, today, days);
            return empty with { Totals = empty.Totals with { BotVisits = botVisits } };
        }

        var events = await LoadEventsAsync(start, end, query.IncludeBots, cancellationToken);
        var paidByOrderNumber = await LoadPaidOrdersAsync(events, cancellationToken);
        var pages = await LoadPagesAsync(start, end, query.IncludeBots, sessions, cancellationToken);
        var dogSittingVisits = await CountDogSittingVisitsAsync(start, end, query.IncludeBots, cancellationToken);
        var who = await LoadVisitorNamesAsync(sessions, cancellationToken);

        return new InsightsReport(
            from,
            today,
            days,
            BuildTotals(sessions, events, paidByOrderNumber, botVisits),
            BuildSources(sessions, events, paidByOrderNumber),
            BuildFunnel(sessions, events, paidByOrderNumber, dogSittingVisits),
            BuildBreakdown(sessions, s => VisitSignals.DeviceLabel(s.Device), null),
            BuildBreakdown(sessions, s => s.Browser ?? VisitSignals.Unknown, s => s.BrowserVersion),
            BuildBreakdown(sessions, s => s.Platform ?? VisitSignals.Unknown, s => s.PlatformVersion),
            BuildBreakdown(sessions, s => ViewportBucket(s.ViewportWidth), null),
            pages,
            BuildTrend(sessions, from, today, offset),
            BuildHours(sessions, offset),
            BuildRecent(sessions, who));
    }

    private async Task<List<SessionRow>> LoadSessionsAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        bool includeBots,
        CancellationToken cancellationToken)
    {
        var rows = _db.VisitSessions
            .AsNoTracking()
            .Where(s => s.StartedAt >= start && s.StartedAt < end);

        if (!includeBots)
        {
            rows = rows.Where(s => !s.IsBot);
        }

        return await rows
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new SessionRow(
                s.Id,
                s.VisitorId,
                s.IpHash,
                s.Source,
                s.FirstTouchSource,
                s.StartedAt,
                EF.Functions.DateDiffSecond(s.StartedAt, s.LastSeenAt),
                s.PageViews,
                s.IsFirstVisit,
                s.Device,
                s.Browser,
                s.BrowserVersion,
                s.Platform,
                s.PlatformVersion,
                s.ViewportWidth,
                s.LandingPath,
                s.ExitPath,
                s.UserId,
                s.IpNetwork))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<EventRow>> LoadEventsAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        bool includeBots,
        CancellationToken cancellationToken)
    {
        var rows = _db.VisitEvents
            .AsNoTracking()
            .Where(e => e.Kind != VisitEventKind.PageView
                && e.Session.StartedAt >= start
                && e.Session.StartedAt < end);

        if (!includeBots)
        {
            rows = rows.Where(e => !e.Session.IsBot);
        }

        return await rows
            .Select(e => new EventRow(e.VisitSessionId, e.Kind, e.Detail, e.Value, e.Session.Source))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<string, decimal>> LoadPaidOrdersAsync(
        List<EventRow> events,
        CancellationToken cancellationToken)
    {
        var numbers = events
            .Where(e => e.Kind == VisitEventKind.OrderPlaced && !string.IsNullOrWhiteSpace(e.Detail))
            .Select(e => e.Detail!)
            .Distinct()
            .ToList();

        if (numbers.Count == 0)
        {
            return [];
        }

        var paid = await _db.Orders
            .AsNoTracking()
            .Where(o => numbers.Contains(o.OrderNumber) && PaidStatuses.Contains(o.Status))
            .Select(o => new { o.OrderNumber, o.Total })
            .ToListAsync(cancellationToken);

        return paid
            .GroupBy(o => o.OrderNumber)
            .ToDictionary(g => g.Key, g => g.Sum(o => o.Total));
    }

    private async Task<List<InsightsPageRow>> LoadPagesAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        bool includeBots,
        List<SessionRow> sessions,
        CancellationToken cancellationToken)
    {
        var rows = _db.VisitEvents
            .AsNoTracking()
            .Where(e => e.Kind == VisitEventKind.PageView
                && e.Session.StartedAt >= start
                && e.Session.StartedAt < end);

        if (!includeBots)
        {
            rows = rows.Where(e => !e.Session.IsBot);
        }

        var grouped = await rows
            .GroupBy(e => e.Path)
            .Select(g => new { Path = g.Key, Views = g.Count() })
            .OrderByDescending(g => g.Views)
            .Take(TopPageLimit)
            .ToListAsync(cancellationToken);

        var visitsPerPage = await rows
            .Select(e => new { e.Path, e.VisitSessionId })
            .Distinct()
            .GroupBy(e => e.Path)
            .Select(g => new { Path = g.Key, Visits = g.Count() })
            .ToListAsync(cancellationToken);

        var visits = visitsPerPage.ToDictionary(g => g.Path, g => g.Visits);

        var entries = sessions
            .GroupBy(s => s.LandingPath)
            .ToDictionary(g => g.Key, g => g.Count());

        var exits = sessions
            .Where(s => s.ExitPath is not null)
            .GroupBy(s => s.ExitPath!)
            .ToDictionary(g => g.Key, g => g.Count());

        return grouped
            .Select(g => new InsightsPageRow(
                g.Path,
                g.Views,
                visits.GetValueOrDefault(g.Path),
                entries.GetValueOrDefault(g.Path),
                exits.GetValueOrDefault(g.Path)))
            .ToList();
    }

    private async Task<int> CountDogSittingVisitsAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        bool includeBots,
        CancellationToken cancellationToken)
    {
        var rows = _db.VisitEvents
            .AsNoTracking()
            .Where(e => e.Kind == VisitEventKind.PageView
                && e.Path.Contains("dog-sitting")
                && e.Session.StartedAt >= start
                && e.Session.StartedAt < end);

        if (!includeBots)
        {
            rows = rows.Where(e => !e.Session.IsBot);
        }

        return await rows
            .Select(e => e.VisitSessionId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    private async Task<Dictionary<string, string>> LoadVisitorNamesAsync(
        List<SessionRow> sessions,
        CancellationToken cancellationToken)
    {
        var ids = sessions
            .Take(RecentVisitLimit)
            .Where(s => !string.IsNullOrWhiteSpace(s.UserId))
            .Select(s => s.UserId!)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        var people = await _db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .ToListAsync(cancellationToken);

        return people.ToDictionary(
            p => p.Id,
            p => string.IsNullOrWhiteSpace(p.DisplayName) ? p.Email ?? "Signed in" : p.DisplayName!);
    }

    private static InsightsTotals BuildTotals(
        List<SessionRow> sessions,
        List<EventRow> events,
        Dictionary<string, decimal> paid,
        int botVisits)
    {
        var placed = events.Where(e => e.Kind == VisitEventKind.OrderPlaced).ToList();

        return new InsightsTotals(
            sessions.Count,
            sessions.Select(s => s.VisitorId).Distinct().Count(),
            sessions.Where(s => s.IpHash.Length > 0).Select(s => s.IpHash).Distinct().Count(),
            sessions.Sum(s => s.PageViews),
            sessions.Count(s => s.IsFirstVisit),
            sessions.Count(s => !s.IsFirstVisit),
            sessions.Count(s => s.PageViews <= 1),
            sessions.Sum(s => s.Seconds),
            events.Count(e => e.Kind == VisitEventKind.Registered),
            placed.Count,
            placed.Sum(e => e.Value ?? 0),
            placed.Where(e => e.Detail is not null).Sum(e => paid.GetValueOrDefault(e.Detail!)),
            events.Count(e => e.Kind == VisitEventKind.BookingBlockedPending),
            botVisits);
    }

    private static List<InsightsSourceRow> BuildSources(
        List<SessionRow> sessions,
        List<EventRow> events,
        Dictionary<string, decimal> paid)
    {
        var bySource = events.GroupBy(e => e.Source).ToDictionary(g => g.Key, g => g.ToList());

        return sessions
            .GroupBy(s => string.IsNullOrWhiteSpace(s.Source) ? VisitSignals.Direct : s.Source)
            .Select(group =>
            {
                var sourceEvents = bySource.GetValueOrDefault(group.Key) ?? [];
                var placed = sourceEvents.Where(e => e.Kind == VisitEventKind.OrderPlaced).ToList();

                return new InsightsSourceRow(
                    group.Key,
                    group.Count(),
                    group.Select(s => s.VisitorId).Distinct().Count(),
                    group.Sum(s => s.PageViews),
                    group.Count(s => s.PageViews <= 1),
                    sourceEvents.Count(e => e.Kind == VisitEventKind.Registered),
                    placed.Count,
                    placed.Where(e => e.Detail is not null).Sum(e => paid.GetValueOrDefault(e.Detail!)));
            })
            .OrderByDescending(row => row.Visits)
            .ToList();
    }

    private static List<InsightsFunnelStep> BuildFunnel(
        List<SessionRow> sessions,
        List<EventRow> events,
        Dictionary<string, decimal> paid,
        int dogSittingVisits)
    {
        int Reached(VisitEventKind kind) =>
            events.Where(e => e.Kind == kind).Select(e => e.SessionId).Distinct().Count();

        var placedNumbers = events
            .Where(e => e.Kind == VisitEventKind.OrderPlaced && e.Detail is not null)
            .Select(e => e.Detail!)
            .Distinct()
            .ToList();

        return
        [
            new InsightsFunnelStep("Visited the site", sessions.Count),
            new InsightsFunnelStep("Looked at dog sitting", dogSittingVisits),
            new InsightsFunnelStep("Signed in", sessions.Count(s => !string.IsNullOrWhiteSpace(s.UserId))),
            new InsightsFunnelStep("Added to basket", Reached(VisitEventKind.AddedToCart)),
            new InsightsFunnelStep("Opened checkout", Reached(VisitEventKind.CheckoutOpened)),
            new InsightsFunnelStep("Placed an order", Reached(VisitEventKind.OrderPlaced)),
            new InsightsFunnelStep("Paid", placedNumbers.Count(paid.ContainsKey))
        ];
    }

    private static List<InsightsBreakdownRow> BuildBreakdown(
        List<SessionRow> sessions,
        Func<SessionRow, string> label,
        Func<SessionRow, string?>? detail)
    {
        return sessions
            .GroupBy(label)
            .Select(group => new InsightsBreakdownRow(
                group.Key,
                detail is null ? null : TopDetail(group, detail),
                group.Count()))
            .OrderByDescending(row => row.Visits)
            .ToList();

        static string? TopDetail(IEnumerable<SessionRow> rows, Func<SessionRow, string?> pick)
        {
            var versions = rows
                .Select(pick)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .GroupBy(value => value!)
                .OrderByDescending(group => group.Count())
                .Take(3)
                .Select(group => group.Key)
                .ToList();

            return versions.Count == 0 ? null : string.Join(", ", versions);
        }
    }

    private static List<InsightsDayPoint> BuildTrend(
        List<SessionRow> sessions,
        DateOnly from,
        DateOnly to,
        TimeSpan offset)
    {
        var byDay = sessions
            .GroupBy(s => DateOnly.FromDateTime(s.StartedAt.ToOffset(offset).DateTime))
            .ToDictionary(g => g.Key, g => (Visits: g.Count(), Views: g.Sum(s => s.PageViews)));

        var points = new List<InsightsDayPoint>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var found = byDay.GetValueOrDefault(day);
            points.Add(new InsightsDayPoint(day, found.Visits, found.Views));
        }

        return points;
    }

    private static List<InsightsHourPoint> BuildHours(List<SessionRow> sessions, TimeSpan offset)
    {
        var byHour = sessions
            .GroupBy(s => s.StartedAt.ToOffset(offset).Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        return Enumerable.Range(0, 24)
            .Select(hour => new InsightsHourPoint(hour, byHour.GetValueOrDefault(hour)))
            .ToList();
    }

    private static List<InsightsVisitRow> BuildRecent(
        List<SessionRow> sessions,
        Dictionary<string, string> who)
    {
        return sessions
            .Take(RecentVisitLimit)
            .Select(s => new InsightsVisitRow(
                s.StartedAt,
                string.IsNullOrWhiteSpace(s.Source) ? VisitSignals.Direct : s.Source,
                s.LandingPath,
                s.ExitPath,
                s.PageViews,
                s.Seconds,
                s.Device,
                s.Browser,
                s.BrowserVersion,
                s.Platform,
                s.PlatformVersion,
                s.ViewportWidth,
                s.IsFirstVisit,
                s.UserId is null ? null : who.GetValueOrDefault(s.UserId),
                s.IpNetwork))
            .ToList();
    }

    private static string ViewportBucket(int? width) => width switch
    {
        null => "Not reported",
        < 400 => "Under 400 px",
        < 768 => "400–767 px",
        < 1024 => "768–1023 px",
        < 1440 => "1024–1439 px",
        _ => "1440 px and wider"
    };

    private sealed record SessionRow(
        int Id,
        Guid VisitorId,
        string IpHash,
        string Source,
        string FirstTouchSource,
        DateTimeOffset StartedAt,
        int Seconds,
        int PageViews,
        bool IsFirstVisit,
        VisitDevice Device,
        string? Browser,
        string? BrowserVersion,
        string? Platform,
        string? PlatformVersion,
        int? ViewportWidth,
        string LandingPath,
        string? ExitPath,
        string? UserId,
        string? IpNetwork);

    private sealed record EventRow(
        int SessionId,
        VisitEventKind Kind,
        string? Detail,
        decimal? Value,
        string Source);
}
