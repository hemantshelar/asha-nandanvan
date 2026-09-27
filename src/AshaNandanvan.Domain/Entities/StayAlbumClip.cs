namespace AshaNandanvan.Domain.Entities;

public class StayAlbumClip
{
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public StayAlbum Album { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string YouTubeVideoId { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateOnly? FilmedOn { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<StayAlbumComment> Comments { get; set; } = new List<StayAlbumComment>();
    public ICollection<StayAlbumReaction> Reactions { get; set; } = new List<StayAlbumReaction>();
}
