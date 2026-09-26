namespace AshaNandanvan.Application.Media;

public interface IMediaService
{
    Task<IReadOnlyList<MediaClip>> GetPublishedAsync(string? offerSlug = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MediaClip>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MediaClip?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(MediaEditModel model, CancellationToken cancellationToken = default);
    Task UpdateAsync(MediaEditModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
