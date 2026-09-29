using System.Security.Cryptography;
using AshaNandanvan.Application.Albums;
using AshaNandanvan.Application.Media;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Domain.Enums;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Infrastructure.Albums;

public sealed class StayAlbumService : IStayAlbumService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public StayAlbumService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<int> EnsureForOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.StayAlbums.FirstOrDefaultAsync(a => a.OrderId == orderId, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var order = await LoadStayOrderAsync(db, orderId, cancellationToken);
        if (order is null)
        {
            return 0;
        }

        if (!order.Status.IsApproved() && !order.Status.IsPaid())
        {
            return 0;
        }

        return await CreateAlbumAsync(db, order, cancellationToken);
    }

    public async Task RefreshDefaultTitleAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var album = await db.StayAlbums.FirstOrDefaultAsync(a => a.OrderId == orderId, cancellationToken);
        if (album is null || !album.TitleIsDefault)
        {
            return;
        }

        var order = await LoadStayOrderAsync(db, orderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        album.Title = StayAlbumTitles.FromStay(order);
        album.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task BackfillApprovedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var orders = await db.Orders
            .Include(o => o.Items)
            .Include(o => o.Album)
            .Where(o => o.Album == null)
            .Where(o => o.Items.Any(i => i.StayStartsAt != null))
            .ToListAsync(cancellationToken);

        foreach (var order in orders.Where(o => o.Status.IsApproved() || o.Status.IsPaid()))
        {
            await CreateAlbumAsync(db, order, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<StayAlbumListItem>> ListMineAsync(string userId, bool admin, CancellationToken cancellationToken = default)
    {
        if (admin)
        {
            await BackfillApprovedAsync(cancellationToken);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.StayAlbums
            .AsNoTracking()
            .Include(a => a.Order)
            .ThenInclude(o => o.Items)
            .Include(a => a.Clips)
            .Include(a => a.Members)
            .AsQueryable();

        if (!admin)
        {
            query = query.Where(a => a.Members.Any(m => m.UserId == userId));
        }

        var rows = await query.OrderByDescending(a => a.UpdatedAt).ToListAsync(cancellationToken);
        return rows.Select(album =>
        {
            var member = album.Members.FirstOrDefault(m => m.UserId == userId);
            var lastSeen = member?.LastSeenAt ?? DateTimeOffset.UtcNow;
            var cover = album.Clips.OrderBy(c => c.SortOrder).ThenBy(c => c.Id).FirstOrDefault();
            return new StayAlbumListItem(
                album.Id,
                album.Title,
                StayAlbumTitles.PetName(album.Order),
                StayAlbumTitles.StayLabel(album.Order),
                album.Order.OrderNumber,
                cover?.YouTubeVideoId ?? string.Empty,
                album.Clips.Count,
                member is null ? 0 : album.Clips.Count(c => c.CreatedAt > lastSeen),
                member?.Role);
        }).ToList();
    }

    public async Task<StayAlbumDetail> GetAsync(int albumId, string userId, bool admin, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var album = await LoadAlbumAsync(db, albumId, cancellationToken);
        EnsureAccess(album, userId, admin);

        var member = album.Members.FirstOrDefault(m => m.UserId == userId);
        var lastSeen = member?.LastSeenAt ?? DateTimeOffset.UtcNow;
        if (member is not null)
        {
            member.LastSeenAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        var userIds = album.Clips.SelectMany(c => c.Comments.Select(m => m.UserId))
            .Concat(album.Members.Select(m => m.UserId))
            .Distinct()
            .ToList();
        var people = await PeopleAsync(db, userIds, cancellationToken);
        var isOwner = member?.Role == StayAlbumMemberRole.Owner;
        var canShare = admin || isOwner;

        return new StayAlbumDetail(
            album.Id,
            album.Title,
            canShare,
            admin,
            canShare,
            StayAlbumTitles.PetName(album.Order),
            StayAlbumTitles.StayLabel(album.Order),
            album.Order.OrderNumber,
            $"/albums/join/{album.InviteToken}",
            album.Clips
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Id)
                .Select(c => new StayAlbumClipView(
                    c.Id,
                    c.Title,
                    c.YouTubeVideoId,
                    c.Caption,
                    c.FilmedOn,
                    c.Reactions.Count,
                    c.Reactions.Any(r => r.UserId == userId),
                    c.CreatedAt > lastSeen,
                    c.Comments
                        .OrderBy(m => m.CreatedAt)
                        .Select(m => new StayAlbumCommentView(
                            m.Id,
                            people.GetValueOrDefault(m.UserId)?.Name ?? "Guest",
                            m.Body,
                            m.CreatedAt,
                            m.UserId == userId))
                        .ToList()))
                .ToList(),
            album.Members
                .OrderBy(m => m.Role)
                .ThenBy(m => m.JoinedAt)
                .Select(m => new StayAlbumMemberView(
                    m.UserId,
                    people.GetValueOrDefault(m.UserId)?.Name ?? "Guest",
                    people.GetValueOrDefault(m.UserId)?.Email ?? string.Empty,
                    m.Role,
                    m.JoinedAt))
                .ToList());
    }

    public async Task RenameAsync(int albumId, string userId, bool admin, string title, CancellationToken cancellationToken = default)
    {
        var trimmed = title.Trim();
        if (trimmed.Length is < 2 or > 160)
        {
            throw new InvalidOperationException("Give the album a short name.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var album = await LoadAlbumAsync(db, albumId, cancellationToken);
        EnsureOwnerOrAdmin(album, userId, admin);
        album.Title = trimmed;
        album.TitleIsDefault = false;
        album.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddClipAsync(int albumId, string userId, bool admin, StayAlbumClipEdit model, CancellationToken cancellationToken = default)
    {
        if (!admin)
        {
            throw new InvalidOperationException("Only Asha can add clips.");
        }

        if (!YouTubeLinks.TryParseVideoId(model.SourceUrl, out var videoId))
        {
            throw new InvalidOperationException("Paste a YouTube watch, Shorts, or youtu.be link.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var album = await LoadAlbumAsync(db, albumId, cancellationToken);
        EnsureAccess(album, userId, admin);

        if (album.Clips.Any(c => c.YouTubeVideoId == videoId))
        {
            throw new InvalidOperationException("That clip is already in this album.");
        }

        var title = string.IsNullOrWhiteSpace(model.Title)
            ? StayAlbumTitles.PetName(album.Order)
            : model.Title.Trim();
        var caption = string.IsNullOrWhiteSpace(model.Caption) ? null : model.Caption.Trim();
        DateOnly? filmedOn = model.FilmedOn is DateTime day ? DateOnly.FromDateTime(day) : null;

        album.Clips.Add(new StayAlbumClip
        {
            Title = title,
            SourceUrl = YouTubeLinks.WatchUrl(videoId),
            YouTubeVideoId = videoId,
            Caption = caption,
            FilmedOn = filmedOn,
            SortOrder = album.Clips.Count == 0 ? 0 : album.Clips.Max(c => c.SortOrder) + 1,
            CreatedAt = DateTimeOffset.UtcNow
        });
        album.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteClipAsync(int albumId, int clipId, string userId, bool admin, CancellationToken cancellationToken = default)
    {
        if (!admin)
        {
            throw new InvalidOperationException("Only Asha can remove clips.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var album = await LoadAlbumAsync(db, albumId, cancellationToken);
        EnsureAccess(album, userId, admin);
        var clip = album.Clips.FirstOrDefault(c => c.Id == clipId)
            ?? throw new InvalidOperationException("That clip was not found.");
        db.StayAlbumClips.Remove(clip);
        album.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddCommentAsync(int albumId, int clipId, string userId, bool admin, string body, CancellationToken cancellationToken = default)
    {
        var text = body.Trim();
        if (text.Length is < 1 or > 1000)
        {
            throw new InvalidOperationException("Write a short comment.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var album = await LoadAlbumAsync(db, albumId, cancellationToken);
        EnsureAccess(album, userId, admin);
        var clip = album.Clips.FirstOrDefault(c => c.Id == clipId)
            ?? throw new InvalidOperationException("That clip was not found.");
        clip.Comments.Add(new StayAlbumComment
        {
            UserId = userId,
            Body = text,
            CreatedAt = DateTimeOffset.UtcNow
        });
        album.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCommentAsync(int albumId, int commentId, string userId, bool admin, CancellationToken cancellationToken = default)
    {
        if (!admin)
        {
            throw new InvalidOperationException("Only Asha can remove comments.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var album = await LoadAlbumAsync(db, albumId, cancellationToken);
        EnsureAccess(album, userId, admin);
        var comment = album.Clips.SelectMany(c => c.Comments).FirstOrDefault(c => c.Id == commentId)
            ?? throw new InvalidOperationException("That comment was not found.");
        db.StayAlbumComments.Remove(comment);
        album.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ToggleHeartAsync(int albumId, int clipId, string userId, bool admin, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var album = await LoadAlbumAsync(db, albumId, cancellationToken);
        EnsureAccess(album, userId, admin);
        var clip = album.Clips.FirstOrDefault(c => c.Id == clipId)
            ?? throw new InvalidOperationException("That clip was not found.");
        var existing = clip.Reactions.FirstOrDefault(r => r.UserId == userId);
        if (existing is null)
        {
            clip.Reactions.Add(new StayAlbumReaction
            {
                UserId = userId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            db.StayAlbumReactions.Remove(existing);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StayAlbumInvitePreview?> PeekInviteAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var album = await db.StayAlbums
            .AsNoTracking()
            .Include(a => a.Order)
            .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(a => a.InviteToken == token, cancellationToken);
        if (album is null)
        {
            return null;
        }

        return new StayAlbumInvitePreview(
            StayAlbumTitles.PetName(album.Order),
            StayAlbumTitles.StayLabel(album.Order));
    }

    public async Task<int> JoinAsync(string token, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("That invite link is not valid.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var album = await db.StayAlbums
            .Include(a => a.Members)
            .FirstOrDefaultAsync(a => a.InviteToken == token, cancellationToken)
            ?? throw new InvalidOperationException("That invite link is not valid or has been replaced.");

        if (album.Members.All(m => m.UserId != userId))
        {
            var now = DateTimeOffset.UtcNow;
            album.Members.Add(new StayAlbumMember
            {
                UserId = userId,
                Role = StayAlbumMemberRole.Guest,
                JoinedAt = now,
                LastSeenAt = now
            });
            album.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        return album.Id;
    }

    public async Task<string> RotateInviteAsync(int albumId, string userId, bool admin, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var album = await LoadAlbumAsync(db, albumId, cancellationToken);
        EnsureOwnerOrAdmin(album, userId, admin);
        album.InviteToken = NewToken();
        album.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return $"/albums/join/{album.InviteToken}";
    }

    public async Task RemoveGuestAsync(int albumId, string guestUserId, string actorUserId, bool admin, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var album = await LoadAlbumAsync(db, albumId, cancellationToken);
        EnsureOwnerOrAdmin(album, actorUserId, admin);
        var guest = album.Members.FirstOrDefault(m => m.UserId == guestUserId)
            ?? throw new InvalidOperationException("That person is not on this album.");
        if (guest.Role == StayAlbumMemberRole.Owner)
        {
            throw new InvalidOperationException("The pet owner cannot be removed.");
        }

        db.StayAlbumMembers.Remove(guest);
        album.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<int> CreateAlbumAsync(AppDbContext db, Order order, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var album = new StayAlbum
        {
            OrderId = order.Id,
            Title = StayAlbumTitles.FromStay(order),
            TitleIsDefault = true,
            InviteToken = NewToken(),
            CreatedAt = now,
            UpdatedAt = now
        };
        album.Members.Add(new StayAlbumMember
        {
            UserId = order.UserId,
            Role = StayAlbumMemberRole.Owner,
            JoinedAt = now,
            LastSeenAt = now
        });
        db.StayAlbums.Add(album);
        await db.SaveChangesAsync(cancellationToken);
        return album.Id;
    }

    private static async Task<Order?> LoadStayOrderAsync(AppDbContext db, int orderId, CancellationToken cancellationToken) =>
        await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(
                o => o.Id == orderId && o.Items.Any(i => i.StayStartsAt != null),
                cancellationToken);

    private static async Task<StayAlbum> LoadAlbumAsync(AppDbContext db, int albumId, CancellationToken cancellationToken) =>
        await db.StayAlbums
            .Include(a => a.Order)
            .ThenInclude(o => o.Items)
            .Include(a => a.Members)
            .Include(a => a.Clips)
            .ThenInclude(c => c.Comments)
            .Include(a => a.Clips)
            .ThenInclude(c => c.Reactions)
            .FirstOrDefaultAsync(a => a.Id == albumId, cancellationToken)
        ?? throw new InvalidOperationException("That album was not found.");

    private static void EnsureAccess(StayAlbum album, string userId, bool admin)
    {
        if (admin || album.Members.Any(m => m.UserId == userId))
        {
            return;
        }

        throw new InvalidOperationException("This album is private.");
    }

    private static void EnsureOwnerOrAdmin(StayAlbum album, string userId, bool admin)
    {
        if (admin || album.Members.Any(m => m.UserId == userId && m.Role == StayAlbumMemberRole.Owner))
        {
            return;
        }

        throw new InvalidOperationException("Only the pet owner or Asha can do that.");
    }

    private static async Task<Dictionary<string, AlbumPerson>> PeopleAsync(
        AppDbContext db,
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        return await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(
                u => u.Id,
                u => new AlbumPerson(
                    string.IsNullOrWhiteSpace(u.DisplayName) ? EmailLocal(u.Email) : u.DisplayName!,
                    u.Email ?? string.Empty),
                cancellationToken);
    }

    private sealed record AlbumPerson(string Name, string Email);

    private static string EmailLocal(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "Guest";
        }

        var at = email.IndexOf('@');
        return at > 0 ? email[..at] : email;
    }

    private static string NewToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(18);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
