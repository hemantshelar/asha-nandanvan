namespace AshaNandanvan.Application.DogSitting;

public interface IDogSittingService
{
    Task<DogSittingSettingsModel> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(DogSittingSettingsModel model, CancellationToken cancellationToken = default);
    Task<StayAvailability> CheckAvailabilityAsync(
        DateTimeOffset dropOff,
        DateTimeOffset pickUp,
        int dogs = 1,
        bool allowPastDropOff = false,
        CancellationToken cancellationToken = default);
}
