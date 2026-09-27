using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Domain.Entities;

public class StayAlbumMember
{
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public StayAlbum Album { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public StayAlbumMemberRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}
