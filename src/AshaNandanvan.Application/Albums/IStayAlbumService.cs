namespace AshaNandanvan.Application.Albums;

public interface IStayAlbumService
{
    Task<int> EnsureForOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task RefreshDefaultTitleAsync(int orderId, CancellationToken cancellationToken = default);
    Task BackfillApprovedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StayAlbumListItem>> ListMineAsync(string userId, bool admin, CancellationToken cancellationToken = default);
    Task<StayAlbumDetail> GetAsync(int albumId, string userId, bool admin, CancellationToken cancellationToken = default);
    Task RenameAsync(int albumId, string userId, bool admin, string title, CancellationToken cancellationToken = default);
    Task AddClipAsync(int albumId, string userId, bool admin, StayAlbumClipEdit model, CancellationToken cancellationToken = default);
    Task DeleteClipAsync(int albumId, int clipId, string userId, bool admin, CancellationToken cancellationToken = default);
    Task AddCommentAsync(int albumId, int clipId, string userId, bool admin, string body, CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(int albumId, int commentId, string userId, bool admin, CancellationToken cancellationToken = default);
    Task ToggleHeartAsync(int albumId, int clipId, string userId, bool admin, CancellationToken cancellationToken = default);
    Task<StayAlbumInvitePreview?> PeekInviteAsync(string token, CancellationToken cancellationToken = default);
    Task<int> JoinAsync(string token, string userId, CancellationToken cancellationToken = default);
    Task<string> RotateInviteAsync(int albumId, string userId, bool admin, CancellationToken cancellationToken = default);
    Task RemoveGuestAsync(int albumId, string guestUserId, string actorUserId, bool admin, CancellationToken cancellationToken = default);
}
