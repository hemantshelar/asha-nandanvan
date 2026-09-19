using AshaNandanvan.Application.Storage;

namespace AshaNandanvan.Web.Services;

public sealed class FileImageStorage : IImageStorage
{
    private readonly IWebHostEnvironment _environment;

    public FileImageStorage(IWebHostEnvironment environment) => _environment = environment;

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var uploads = Path.Combine(_environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploads);

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || extension.Length > 8)
        {
            extension = ".jpg";
        }

        var stored = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = Path.Combine(uploads, stored);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
        return $"/uploads/{stored}";
    }
}
