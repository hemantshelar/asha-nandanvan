namespace AshaNandanvan.Application.Media;

public interface IMediaService
{
    Task<IReadOnlyList<MediaClip>> ListAsync(string? offerSlug = null, bool publishedOnly = true, CancellationToken cancellationToken = default);
    Task SaveAsync(MediaEditModel model, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
