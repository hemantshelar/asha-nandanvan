namespace AshaNandanvan.Domain.Entities;

public class StayAlbum
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public bool TitleIsDefault { get; set; } = true;
    public string InviteToken { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<StayAlbumClip> Clips { get; set; } = new List<StayAlbumClip>();
    public ICollection<StayAlbumMember> Members { get; set; } = new List<StayAlbumMember>();
}
