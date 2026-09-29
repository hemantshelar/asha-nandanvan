namespace AshaNandanvan.Application.Users;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserAdminListItem>> ListAsync(
        string actorUserId,
        UserAdminListQuery query,
        CancellationToken cancellationToken = default);

    Task<UserAdminDetail> GetAsync(
        string actorUserId,
        string userId,
        CancellationToken cancellationToken = default);

    Task BlockAsync(
        string actorUserId,
        string userId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task UnblockAsync(
        string actorUserId,
        string userId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string actorUserId,
        string userId,
        CancellationToken cancellationToken = default);

    Task AssignStayPlanAsync(
        string actorUserId,
        string userId,
        int? planId,
        CancellationToken cancellationToken = default);
}
