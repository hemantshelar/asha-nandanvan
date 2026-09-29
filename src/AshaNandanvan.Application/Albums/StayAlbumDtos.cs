using AshaNandanvan.Application.Media;
using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Application.Albums;

public sealed record StayAlbumListItem(
    int Id,
    string Title,
    string PetName,
    string StayLabel,
    string OrderNumber,
    string CoverYouTubeId,
    int ClipCount,
    int NewClipCount,
    StayAlbumMemberRole? Role)
{
    public string? CoverThumbnailUrl =>
        string.IsNullOrWhiteSpace(CoverYouTubeId) ? null : YouTubeLinks.ThumbnailUrl(CoverYouTubeId);
}

public sealed record StayAlbumDetail(
    int Id,
    string Title,
    bool CanRename,
    bool CanManage,
    bool CanShare,
    string PetName,
    string StayLabel,
    string OrderNumber,
    string InvitePath,
    IReadOnlyList<StayAlbumClipView> Clips,
    IReadOnlyList<StayAlbumMemberView> Members);

public sealed record StayAlbumClipView(
    int Id,
    string Title,
    string YouTubeVideoId,
    string? Caption,
    DateOnly? FilmedOn,
    int HeartCount,
    bool YouHearted,
    bool IsNew,
    IReadOnlyList<StayAlbumCommentView> Comments)
{
    public string ThumbnailUrl => YouTubeLinks.ThumbnailUrl(YouTubeVideoId);
    public string EmbedUrl => YouTubeLinks.EmbedUrl(YouTubeVideoId);
}

public sealed record StayAlbumCommentView(
    int Id,
    string Author,
    string Body,
    DateTimeOffset CreatedAt,
    bool IsYours);

public sealed record StayAlbumMemberView(
    string UserId,
    string Name,
    string Email,
    StayAlbumMemberRole Role,
    DateTimeOffset JoinedAt);

public sealed class StayAlbumClipEdit
{
    public string SourceUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public DateTime? FilmedOn { get; set; }
}

public sealed record StayAlbumInvitePreview(string PetName, string StayLabel);
