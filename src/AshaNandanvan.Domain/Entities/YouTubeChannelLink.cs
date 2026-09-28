namespace AshaNandanvan.Domain.Entities;

public class YouTubeChannelLink
{
    public int Id { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelTitle { get; set; } = string.Empty;
    public string ConnectedByUserId { get; set; } = string.Empty;
    public DateTimeOffset ConnectedAt { get; set; }
}
