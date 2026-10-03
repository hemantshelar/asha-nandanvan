using AshaNandanvan.Application.Analytics;
using AshaNandanvan.Application.Options;
using Microsoft.Extensions.Options;

namespace AshaNandanvan.Web.Services;

/// <summary>Drops visits older than the retention window once a day.</summary>
public sealed class VisitRetentionService : BackgroundService
{
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopes;
    private readonly AnalyticsOptions _options;
    private readonly ILogger<VisitRetentionService> _logger;

    public VisitRetentionService(
        IServiceScopeFactory scopes,
        IOptions<AnalyticsOptions> options,
        ILogger<VisitRetentionService> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || _options.RetentionDays <= 0)
        {
            return;
        }

        try
        {
            await Task.Delay(FirstRunDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await PurgeAsync(stoppingToken);
                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private async Task PurgeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IVisitStore>();
            var cutoff = DateTimeOffset.UtcNow.AddDays(-_options.RetentionDays);
            var removed = await store.PurgeBeforeAsync(cutoff, cancellationToken);

            if (removed > 0)
            {
                _logger.LogInformation("Visit retention removed {Count} visits older than {Days} days.", removed, _options.RetentionDays);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Visit retention pass failed; will try again tomorrow.");
        }
    }
}
