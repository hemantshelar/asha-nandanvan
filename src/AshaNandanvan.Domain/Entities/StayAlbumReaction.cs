namespace AshaNandanvan.Domain.Entities;

public class StayAlbumReaction
{
    public int Id { get; set; }
    public int ClipId { get; set; }
    public StayAlbumClip Clip { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
