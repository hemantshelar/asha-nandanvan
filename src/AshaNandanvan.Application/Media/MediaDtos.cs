namespace AshaNandanvan.Application.Media;

public sealed record MediaClip(
    int Id,
    string OfferSlug,
    string OfferTitle,
    string OfferEyebrow,
    string Title,
    string SourceUrl,
    string YouTubeVideoId,
    string ThumbnailUrl,
    string EmbedUrl,
    int SortOrder,
    bool IsPublished);

public sealed class MediaEditModel
{
    public int? Id { get; set; }
    public string OfferSlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}
