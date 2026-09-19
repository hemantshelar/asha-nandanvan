namespace AshaNandanvan.Application.Storage;

public interface IImageStorage
{
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}
