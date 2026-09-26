namespace AshaNandanvan.Domain.Entities;

public class MediaItem
{
    public int Id { get; set; }
    public string OfferSlug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string YouTubeVideoId { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }
}
