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

    public async Task<IReadOnlyList<MediaClip>> GetPublishedAsync(string? offerSlug = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.MediaItems.AsNoTracking().Where(m => m.IsPublished);
        if (!string.IsNullOrWhiteSpace(offerSlug))
        {
            query = query.Where(m => m.OfferSlug == offerSlug);
        }

        var items = await query
            .OrderBy(m => m.SortOrder)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(ToClip).Where(clip => clip is not null).Cast<MediaClip>().ToList();
    }

    public async Task<IReadOnlyList<MediaClip>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var items = await db.MediaItems.AsNoTracking()
            .OrderBy(m => m.OfferSlug)
            .ThenBy(m => m.SortOrder)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(ToClip).Where(clip => clip is not null).Cast<MediaClip>().ToList();
    }

    public async Task<MediaClip?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.MediaItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        return item is null ? null : ToClip(item);
    }

    public async Task<int> CreateAsync(MediaEditModel model, CancellationToken cancellationToken = default)
    {
        var (offer, videoId, title) = Validate(model);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var item = new MediaItem
        {
            OfferSlug = offer.Slug,
            Title = title,
            SourceUrl = model.SourceUrl.Trim(),
            YouTubeVideoId = videoId,
            SortOrder = model.SortOrder,
            IsPublished = model.IsPublished,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.MediaItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task UpdateAsync(MediaEditModel model, CancellationToken cancellationToken = default)
    {
        if (model.Id is not int id)
        {
            throw new InvalidOperationException("That clip was not found.");
        }

        var (offer, videoId, title) = Validate(model);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.MediaItems.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("That clip was not found.");

        item.OfferSlug = offer.Slug;
        item.Title = title;
        item.SourceUrl = model.SourceUrl.Trim();
        item.YouTubeVideoId = videoId;
        item.SortOrder = model.SortOrder;
        item.IsPublished = model.IsPublished;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.MediaItems.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("That clip was not found.");
        db.MediaItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
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

    private static MediaClip? ToClip(MediaItem item)
    {
        var offer = OfferCatalog.Find(item.OfferSlug);
        if (offer is null)
        {
            return null;
        }

        return new MediaClip(
            item.Id,
            offer.Slug,
            offer.Title,
            offer.Eyebrow,
            item.Title,
            item.SourceUrl,
            item.YouTubeVideoId,
            YouTubeLinks.ThumbnailUrl(item.YouTubeVideoId),
            YouTubeLinks.EmbedUrl(item.YouTubeVideoId),
            item.SortOrder,
            item.IsPublished);
    }
}
