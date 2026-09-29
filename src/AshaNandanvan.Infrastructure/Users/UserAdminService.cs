using AshaNandanvan.Application.Common;
using AshaNandanvan.Application.Users;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Domain.Enums;
using AshaNandanvan.Infrastructure.Data;
using AshaNandanvan.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Infrastructure.Users;

public sealed class UserAdminService : IUserAdminService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public UserAdminService(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IReadOnlyList<UserAdminListItem>> ListAsync(
        string actorUserId,
        UserAdminListQuery query,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUserId);
        await BackfillAsync(cancellationToken);

        var rows = await BuildRowsAsync(cancellationToken);
        IEnumerable<UserAdminListItem> filtered = rows;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            filtered = filtered.Where(u =>
                u.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || u.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Source is UserSignupSource source)
        {
            filtered = filtered.Where(u => u.SignupSource == source);
        }

        if (query.Blocked is bool blocked)
        {
            filtered = filtered.Where(u => u.IsBlocked == blocked);
        }

        if (query.GuestsOnly)
        {
            filtered = filtered.Where(u => u.Activity == UserActivityKind.GuestOnly);
        }

        return filtered
            .OrderByDescending(u => u.SignedUpAt)
            .ThenBy(u => u.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<UserAdminDetail> GetAsync(
        string actorUserId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUserId);
        await BackfillAsync(cancellationToken);

        var user = await _users.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("That person was not found.");

        var row = (await BuildRowsAsync(cancellationToken, userId)).Single();
        var (canModerate, blockReason) = await ModerateStateAsync(actorUserId, user);

        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new { o.OrderNumber, o.Status, o.Total, o.CreatedAt })
            .ToListAsync(cancellationToken);

        var albums = await _db.StayAlbumMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => new { m.AlbumId, m.Album.Title, m.Role, m.LastSeenAt })
            .OrderByDescending(m => m.LastSeenAt)
            .ToListAsync(cancellationToken);

        return new UserAdminDetail(
            row.Id,
            row.Name,
            row.Email,
            row.SignupSource,
            row.Activity,
            row.LaterBookedStay,
            row.SignInProviders,
            row.OrderCount,
            row.OwnedAlbumCount,
            row.GuestAlbumCount,
            row.IsBlocked,
            row.SignedUpAt,
            row.BlockedAt,
            user.BlockedReason,
            albums.Count == 0 ? null : albums.Max(a => a.LastSeenAt),
            canModerate,
            blockReason,
            user.AssignedStayPlanId,
            user.AssignedStayPlanId is int planId
                ? await _db.StayRatePlans.AsNoTracking().Where(p => p.Id == planId).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken)
                : null,
            orders.Select(o => new UserAdminOrderLink(o.OrderNumber, o.Status.DisplayName(), o.Total, o.CreatedAt)).ToList(),
            albums.Select(a => new UserAdminAlbumLink(a.AlbumId, a.Title, a.Role, a.LastSeenAt)).ToList());
    }

    public async Task AssignStayPlanAsync(
        string actorUserId,
        string userId,
        int? planId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminAsync(actorUserId);
        var user = await _users.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("That person was not found.");
        if (planId is int id)
        {
            var exists = await _db.StayRatePlans.AnyAsync(p => p.Id == id, cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException("That stay plan was not found.");
            }
        }

        user.AssignedStayPlanId = planId;
        var result = await _users.UpdateAsync(user);
        EnsureSucceeded(result, "We could not save that stay plan.");
    }

    public async Task BlockAsync(
        string actorUserId,
        string userId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var user = await LoadForModerateAsync(actorUserId, userId);
        var note = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (note is { Length: > 200 })
        {
            throw new InvalidOperationException("Keep the note under 200 characters.");
        }

        user.IsBlocked = true;
        user.BlockedAt = DateTimeOffset.UtcNow;
        user.BlockedReason = note;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        var result = await _users.UpdateAsync(user);
        EnsureSucceeded(result, "We could not pause that account.");
    }

    public async Task UnblockAsync(string actorUserId, string userId, CancellationToken cancellationToken = default)
    {
        var user = await LoadForModerateAsync(actorUserId, userId);
        user.IsBlocked = false;
        user.BlockedAt = null;
        user.BlockedReason = null;
        user.LockoutEnd = null;
        var result = await _users.UpdateAsync(user);
        EnsureSucceeded(result, "We could not reopen that account.");
    }

    public async Task DeleteAsync(string actorUserId, string userId, CancellationToken cancellationToken = default)
    {
        var user = await LoadForModerateAsync(actorUserId, userId);
        var now = DateTimeOffset.UtcNow;

        var leftoverComments = await _db.StayAlbumComments
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
        _db.StayAlbumComments.RemoveRange(leftoverComments);

        var leftoverHearts = await _db.StayAlbumReactions
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken);
        _db.StayAlbumReactions.RemoveRange(leftoverHearts);

        var leftoverMembers = await _db.StayAlbumMembers
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);
        var ownedAlbumIds = leftoverMembers
            .Where(m => m.Role == StayAlbumMemberRole.Owner)
            .Select(m => m.AlbumId)
            .ToList();
        _db.StayAlbumMembers.RemoveRange(leftoverMembers);

        var ownedAlbums = await _db.StayAlbums
            .Where(a => ownedAlbumIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        _db.StayAlbums.RemoveRange(ownedAlbums);

        var carts = await _db.Carts
            .Where(c => c.UserId == userId)
            .ToListAsync(cancellationToken);
        _db.Carts.RemoveRange(carts);

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .ToListAsync(cancellationToken);
        foreach (var order in orders)
        {
            order.UserId = string.Empty;
            order.CustomerName = "Deleted account";
            order.CustomerEmail = "deleted";
            order.Phone = null;
            order.UpdatedAt = now;
            foreach (var item in order.Items)
            {
                if (IsActiveStay(item, now))
                {
                    continue;
                }

                item.PetName = null;
                item.PetBreed = null;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var result = await _users.DeleteAsync(user);
        EnsureSucceeded(result, "We could not delete that account.");
    }

    private async Task<List<UserAdminListItem>> BuildRowsAsync(
        CancellationToken cancellationToken,
        string? onlyUserId = null)
    {
        var usersQuery = _db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(onlyUserId))
        {
            usersQuery = usersQuery.Where(u => u.Id == onlyUserId);
        }

        var users = await usersQuery.ToListAsync(cancellationToken);
        var adminIds = await AdminUserIdsAsync(cancellationToken);
        var logins = await _db.UserLogins.AsNoTracking()
            .Where(l => onlyUserId == null || l.UserId == onlyUserId)
            .ToListAsync(cancellationToken);
        var orderCounts = await _db.Orders.AsNoTracking()
            .Where(o => o.UserId != "")
            .Where(o => onlyUserId == null || o.UserId == onlyUserId)
            .GroupBy(o => o.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var members = await _db.StayAlbumMembers.AsNoTracking()
            .Where(m => onlyUserId == null || m.UserId == onlyUserId)
            .Select(m => new { m.UserId, m.Role })
            .ToListAsync(cancellationToken);

        var orderByUser = orderCounts.ToDictionary(x => x.UserId, x => x.Count);
        var providersByUser = logins
            .GroupBy(l => l.UserId)
            .ToDictionary(g => g.Key, g => g.Select(l => ProviderLabel(l.LoginProvider)).Distinct().OrderBy(p => p).ToList());
        var ownedByUser = members
            .Where(m => m.Role == StayAlbumMemberRole.Owner)
            .GroupBy(m => m.UserId)
            .ToDictionary(g => g.Key, g => g.Count());
        var guestByUser = members
            .Where(m => m.Role == StayAlbumMemberRole.Guest)
            .GroupBy(m => m.UserId)
            .ToDictionary(g => g.Key, g => g.Count());

        return users.Select(user =>
        {
            var orders = orderByUser.GetValueOrDefault(user.Id);
            var owned = ownedByUser.GetValueOrDefault(user.Id);
            var guests = guestByUser.GetValueOrDefault(user.Id);
            var isAdmin = adminIds.Contains(user.Id);
            var activity = isAdmin
                ? UserActivityKind.Admin
                : orders > 0 || owned > 0
                    ? UserActivityKind.HasStays
                    : guests > 0
                        ? UserActivityKind.GuestOnly
                        : UserActivityKind.None;

            return new UserAdminListItem(
                user.Id,
                DisplayName(user),
                user.Email ?? string.Empty,
                user.SignupSource,
                activity,
                user.SignupSource == UserSignupSource.AlbumInvite && orders > 0,
                providersByUser.GetValueOrDefault(user.Id) ?? [],
                orders,
                owned,
                guests,
                user.IsBlocked,
                user.SignedUpAt,
                user.BlockedAt);
        }).ToList();
    }

    private async Task BackfillAsync(CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .Where(u => u.SignupSource == UserSignupSource.Unknown || u.SignedUpAt == default)
            .ToListAsync(cancellationToken);
        if (users.Count == 0)
        {
            return;
        }

        var ids = users.Select(u => u.Id).ToList();
        var firstOrders = await _db.Orders
            .AsNoTracking()
            .Where(o => ids.Contains(o.UserId))
            .GroupBy(o => o.UserId)
            .Select(g => new { UserId = g.Key, At = g.Min(o => o.CreatedAt) })
            .ToListAsync(cancellationToken);
        var firstJoins = await _db.StayAlbumMembers
            .AsNoTracking()
            .Where(m => ids.Contains(m.UserId))
            .GroupBy(m => m.UserId)
            .Select(g => new { UserId = g.Key, At = g.Min(m => m.JoinedAt), Guest = g.Any(m => m.Role == StayAlbumMemberRole.Guest) })
            .ToListAsync(cancellationToken);

        var orderAt = firstOrders.ToDictionary(x => x.UserId, x => x.At);
        var joinAt = firstJoins.ToDictionary(x => x.UserId, x => x.At);
        var guestOnly = firstJoins
            .Where(x => x.Guest && !orderAt.ContainsKey(x.UserId))
            .Select(x => x.UserId)
            .ToHashSet();
        var now = DateTimeOffset.UtcNow;

        foreach (var user in users)
        {
            if (user.SignupSource == UserSignupSource.Unknown)
            {
                user.SignupSource = guestOnly.Contains(user.Id)
                    ? UserSignupSource.AlbumInvite
                    : UserSignupSource.Site;
            }

            if (user.SignedUpAt == default)
            {
                var candidates = new List<DateTimeOffset>();
                if (orderAt.TryGetValue(user.Id, out var ordered))
                {
                    candidates.Add(ordered);
                }

                if (joinAt.TryGetValue(user.Id, out var joined))
                {
                    candidates.Add(joined);
                }

                user.SignedUpAt = candidates.Count == 0 ? now : candidates.Min();
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser> LoadForModerateAsync(string actorUserId, string userId)
    {
        await EnsureAdminAsync(actorUserId);
        var user = await _users.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("That person was not found.");
        var (_, reason) = await ModerateStateAsync(actorUserId, user);
        if (reason is not null)
        {
            throw new InvalidOperationException(reason);
        }

        return user;
    }

    private async Task<(bool CanModerate, string? Reason)> ModerateStateAsync(string actorUserId, ApplicationUser user)
    {
        if (string.Equals(actorUserId, user.Id, StringComparison.Ordinal))
        {
            return (false, "You cannot pause or delete your own account.");
        }

        if (await _users.IsInRoleAsync(user, AppRoles.Admin))
        {
            return (false, "Admin accounts stay in place.");
        }

        return (true, null);
    }

    private async Task EnsureAdminAsync(string actorUserId)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            throw new InvalidOperationException("Please sign in again.");
        }

        var actor = await _users.FindByIdAsync(actorUserId)
            ?? throw new InvalidOperationException("Please sign in again.");
        if (!await _users.IsInRoleAsync(actor, AppRoles.Admin))
        {
            throw new InvalidOperationException("Only Asha can manage people.");
        }
    }

    private async Task<HashSet<string>> AdminUserIdsAsync(CancellationToken cancellationToken)
    {
        var adminRoleId = await _db.Roles
            .Where(r => r.Name == AppRoles.Admin)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (adminRoleId is null)
        {
            return [];
        }

        return (await _db.UserRoles
            .Where(r => r.RoleId == adminRoleId)
            .Select(r => r.UserId)
            .ToListAsync(cancellationToken))
            .ToHashSet();
    }

    private static void EnsureSucceeded(IdentityResult result, string fallback)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Errors.FirstOrDefault()?.Description ?? fallback);
        }
    }

    private static bool IsActiveStay(OrderItem item, DateTimeOffset now) =>
        item.StayStartsAt is DateTimeOffset start
        && item.StayEndsAt is DateTimeOffset end
        && start <= now
        && end >= now;

    private static string DisplayName(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName.Trim();
        }

        var email = user.Email ?? string.Empty;
        var at = email.IndexOf('@');
        return at > 0 ? email[..at] : email;
    }

    private static string ProviderLabel(string provider) => provider switch
    {
        "Google" => "Google",
        "Facebook" => "Facebook",
        _ => provider
    };
}
