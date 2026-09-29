namespace AshaNandanvan.Application.DogSitting;

public interface IStayPlanService
{
    Task<IReadOnlyList<StayRatePlanView>> ListAsync(CancellationToken cancellationToken = default);

    Task SaveRatesAsync(IReadOnlyList<StayRatePlanEdit> plans, CancellationToken cancellationToken = default);

    Task<StayPlanQuote> QuoteAsync(
        string? userId,
        DateTimeOffset dropOff,
        DateTimeOffset pickUp,
        int dogs,
        bool trialStay = false,
        CancellationToken cancellationToken = default);

    Task<int?> GetAssignedPlanIdAsync(string userId, CancellationToken cancellationToken = default);
}
