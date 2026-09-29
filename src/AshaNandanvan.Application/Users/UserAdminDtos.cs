using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Application.Users;

public enum UserActivityKind
{
    None = 0,
    GuestOnly = 1,
    HasStays = 2,
    Admin = 3
}

public sealed record UserAdminListQuery(
    string? Search = null,
    UserSignupSource? Source = null,
    bool? Blocked = null,
    bool GuestsOnly = false);

public sealed record UserAdminListItem(
    string Id,
    string Name,
    string Email,
    UserSignupSource SignupSource,
    UserActivityKind Activity,
    bool LaterBookedStay,
    IReadOnlyList<string> SignInProviders,
    int OrderCount,
    int OwnedAlbumCount,
    int GuestAlbumCount,
    bool IsBlocked,
    DateTimeOffset SignedUpAt,
    DateTimeOffset? BlockedAt);

public sealed record UserAdminOrderLink(
    string OrderNumber,
    string StatusLabel,
    decimal Total,
    DateTimeOffset CreatedAt);

public sealed record UserAdminAlbumLink(
    int Id,
    string Title,
    StayAlbumMemberRole Role,
    DateTimeOffset LastSeenAt);

public sealed record UserAdminDetail(
    string Id,
    string Name,
    string Email,
    UserSignupSource SignupSource,
    UserActivityKind Activity,
    bool LaterBookedStay,
    IReadOnlyList<string> SignInProviders,
    int OrderCount,
    int OwnedAlbumCount,
    int GuestAlbumCount,
    bool IsBlocked,
    DateTimeOffset SignedUpAt,
    DateTimeOffset? BlockedAt,
    string? BlockedReason,
    DateTimeOffset? LastAlbumVisit,
    bool CanModerate,
    string? ModerateBlockReason,
    int? AssignedStayPlanId,
    string? AssignedStayPlanName,
    IReadOnlyList<UserAdminOrderLink> Orders,
    IReadOnlyList<UserAdminAlbumLink> Albums);
