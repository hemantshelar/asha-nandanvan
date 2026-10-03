using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Application.Analytics;

public sealed record InsightsQuery(int Days = 30, bool IncludeBots = false);

public sealed record InsightsTotals(
    int Visits,
    int UniqueVisitors,
    int UniqueNetworks,
    int PageViews,
    int NewVisitors,
    int ReturningVisits,
    int BouncedVisits,
    int TotalSeconds,
    int Registrations,
    int OrdersPlaced,
    decimal RevenuePlaced,
    decimal RevenuePaid,
    int BlockedByPendingOrder,
    int BotVisits)
{
    public double PagesPerVisit => Visits == 0 ? 0 : (double)PageViews / Visits;
    public double BounceRate => Visits == 0 ? 0 : (double)BouncedVisits / Visits;
    public int AverageSeconds => Visits == 0 ? 0 : TotalSeconds / Visits;
    public double VisitToOrderRate => Visits == 0 ? 0 : (double)OrdersPlaced / Visits;
}

public sealed record InsightsSourceRow(
    string Source,
    int Visits,
    int UniqueVisitors,
    int PageViews,
    int BouncedVisits,
    int Registrations,
    int Orders,
    decimal RevenuePaid)
{
    public double BounceRate => Visits == 0 ? 0 : (double)BouncedVisits / Visits;
    public double PagesPerVisit => Visits == 0 ? 0 : (double)PageViews / Visits;
    public double OrderRate => Visits == 0 ? 0 : (double)Orders / Visits;
}

public sealed record InsightsBreakdownRow(string Label, string? Detail, int Visits);

public sealed record InsightsPageRow(string Path, int Views, int Visits, int Entries, int Exits);

public sealed record InsightsDayPoint(DateOnly Day, int Visits, int PageViews);

public sealed record InsightsHourPoint(int Hour, int Visits);

public sealed record InsightsFunnelStep(string Label, int Count)
{
    public double ShareOfTop(int top) => top == 0 ? 0 : (double)Count / top;
}

public sealed record InsightsVisitRow(
    DateTimeOffset At,
    string Source,
    string LandingPath,
    string? ExitPath,
    int PageViews,
    int Seconds,
    VisitDevice Device,
    string? Browser,
    string? BrowserVersion,
    string? Platform,
    string? PlatformVersion,
    int? ViewportWidth,
    bool IsFirstVisit,
    string? Who,
    string? Network);

public sealed record InsightsReport(
    DateOnly From,
    DateOnly To,
    int Days,
    InsightsTotals Totals,
    IReadOnlyList<InsightsSourceRow> Sources,
    IReadOnlyList<InsightsFunnelStep> Funnel,
    IReadOnlyList<InsightsBreakdownRow> Devices,
    IReadOnlyList<InsightsBreakdownRow> Browsers,
    IReadOnlyList<InsightsBreakdownRow> Platforms,
    IReadOnlyList<InsightsBreakdownRow> Viewports,
    IReadOnlyList<InsightsPageRow> Pages,
    IReadOnlyList<InsightsDayPoint> Trend,
    IReadOnlyList<InsightsHourPoint> Hours,
    IReadOnlyList<InsightsVisitRow> Recent)
{
    public static InsightsReport Empty(DateOnly from, DateOnly to, int days) =>
        new(from, to, days,
            new InsightsTotals(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            [], [], [], [], [], [], [], [], [], []);
}
