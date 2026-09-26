namespace AshaNandanvan.Application.DogSitting;

public sealed record DogBreedOption(int Id, string Name, bool OffersSitting, bool IsLargeBreed);

public sealed record DogBreedReviewItem(
    int Id,
    string Name,
    bool IsApproved,
    bool IsRejected,
    bool OffersSitting,
    bool IsLargeBreed,
    DateTimeOffset UpdatedAt)
{
    public string Status =>
        IsRejected ? "Declined" : IsApproved ? "Approved" : "Pending";
}

public interface IDogBreedService
{
    Task<IReadOnlyList<DogBreedOption>> ListApprovedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DogBreedReviewItem>> ListForReviewAsync(CancellationToken cancellationToken = default);
    Task<string> ResolveForBookingAsync(string name, CancellationToken cancellationToken = default);
    Task ApproveAsync(int id, CancellationToken cancellationToken = default);
    Task RejectAsync(int id, CancellationToken cancellationToken = default);
    Task CreateAsync(string name, bool approved = true, bool? offersSitting = null, bool? isLargeBreed = null, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, string name, bool offersSitting, bool isLargeBreed, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
