namespace AshaNandanvan.Application.Media;

public sealed record MediaClip(
    int Id,
    string OfferSlug,
    string OfferEyebrow,
    string Title,
    string YouTubeVideoId,
    int SortOrder,
    bool IsPublished)
{
    public string ThumbnailUrl => YouTubeLinks.ThumbnailUrl(YouTubeVideoId);
    public string EmbedUrl => YouTubeLinks.EmbedUrl(YouTubeVideoId);
    public string WatchUrl => YouTubeLinks.WatchUrl(YouTubeVideoId);
}

public sealed class MediaEditModel
{
    public int? Id { get; set; }
    public string OfferSlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}
