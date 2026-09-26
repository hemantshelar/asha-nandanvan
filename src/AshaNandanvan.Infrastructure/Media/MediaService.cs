using AshaNandanvan.Application.Media;
using AshaNandanvan.Application.Offers;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Infrastructure.Media;

public sealed class MediaService : IMediaService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public MediaService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<IReadOnlyList<MediaClip>> ListAsync(string? offerSlug = null, bool publishedOnly = true, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.MediaItems.AsNoTracking().AsQueryable();
        if (publishedOnly)
        {
            query = query.Where(m => m.IsPublished);
        }

        if (!string.IsNullOrWhiteSpace(offerSlug))
        {
            query = query.Where(m => m.OfferSlug == offerSlug);
        }

        var rows = await query
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Id)
            .Select(m => new { m.Id, m.OfferSlug, m.Title, m.YouTubeVideoId, m.SortOrder, m.IsPublished })
            .ToListAsync(cancellationToken);

        return rows.Select(row => ToClip(row.Id, row.OfferSlug, row.Title, row.YouTubeVideoId, row.SortOrder, row.IsPublished))
            .Where(clip => clip is not null)
            .Cast<MediaClip>()
            .ToList();
    }

    public async Task SaveAsync(MediaEditModel model, CancellationToken cancellationToken = default)
    {
        var (offer, videoId, title) = Validate(model);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var watchUrl = YouTubeLinks.WatchUrl(videoId);
        var duplicate = await db.MediaItems.AnyAsync(
            m => m.OfferSlug == offer.Slug && m.YouTubeVideoId == videoId && m.Id != (model.Id ?? 0),
            cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("That YouTube clip is already in this offer.");
        }

        if (model.Id is int id)
        {
            var item = await db.MediaItems.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("That clip was not found.");
            item.OfferSlug = offer.Slug;
            item.Title = title;
            item.SourceUrl = watchUrl;
            item.YouTubeVideoId = videoId;
            item.SortOrder = model.SortOrder;
            item.IsPublished = model.IsPublished;
            item.UpdatedAt = now;
        }
        else
        {
            db.MediaItems.Add(new MediaItem
            {
                OfferSlug = offer.Slug,
                Title = title,
                SourceUrl = watchUrl,
                YouTubeVideoId = videoId,
                SortOrder = model.SortOrder,
                IsPublished = model.IsPublished,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException(Innermost(ex), ex);
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var removed = await db.MediaItems.Where(m => m.Id == id).ExecuteDeleteAsync(cancellationToken);
        if (removed == 0)
        {
            throw new InvalidOperationException("That clip was not found.");
        }
    }

    private static (OfferDefinition Offer, string VideoId, string Title) Validate(MediaEditModel model)
    {
        var offer = OfferCatalog.Find(model.OfferSlug)
            ?? throw new InvalidOperationException("Pick one of the four offers.");
        if (!YouTubeLinks.TryParseVideoId(model.SourceUrl, out var videoId))
        {
            throw new InvalidOperationException("Paste a YouTube watch, Shorts, or youtu.be link.");
        }

        var title = string.IsNullOrWhiteSpace(model.Title)
            ? $"{offer.Title} clip"
            : model.Title.Trim();
        return (offer, videoId, title);
    }

    private static MediaClip? ToClip(int id, string offerSlug, string title, string videoId, int sortOrder, bool published)
    {
        var offer = OfferCatalog.Find(offerSlug);
        return offer is null
            ? null
            : new MediaClip(id, offer.Slug, offer.Eyebrow, title, videoId, sortOrder, published);
    }

    private static string Innermost(Exception ex)
    {
        while (ex.InnerException is not null)
        {
            ex = ex.InnerException;
        }

        return string.IsNullOrWhiteSpace(ex.Message)
            ? "The clip could not be saved."
            : ex.Message;
    }
}
