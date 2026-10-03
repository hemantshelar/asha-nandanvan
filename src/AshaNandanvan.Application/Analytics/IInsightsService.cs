namespace AshaNandanvan.Application.Analytics;

public interface IInsightsService
{
    Task<InsightsReport> GetAsync(InsightsQuery query, CancellationToken cancellationToken = default);
}
