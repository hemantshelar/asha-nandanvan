using AshaNandanvan.Application.DogSitting;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Infrastructure.DogSitting;

public sealed class DogBreedService : IDogBreedService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DogBreedService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<IReadOnlyList<DogBreedOption>> ListApprovedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.DogBreeds.AsNoTracking()
            .Where(b => b.IsApproved && !b.IsRejected)
            .OrderBy(b => b.Name)
            .Select(b => new DogBreedOption(b.Id, b.Name, b.OffersSitting, b.IsLargeBreed))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DogBreedReviewItem>> ListForReviewAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.DogBreeds.AsNoTracking()
            .OrderBy(b => b.IsRejected)
            .ThenBy(b => b.IsApproved)
            .ThenBy(b => b.Name)
            .Select(b => new DogBreedReviewItem(b.Id, b.Name, b.IsApproved, b.IsRejected, b.OffersSitting, b.IsLargeBreed, b.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<string> ResolveForBookingAsync(string name, CancellationToken cancellationToken = default)
    {
        var breed = Normalize(name);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.DogBreeds.FirstOrDefaultAsync(b => b.Name == breed, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            db.DogBreeds.Add(new DogBreed
            {
                Name = breed,
                IsApproved = false,
                IsRejected = false,
                OffersSitting = DogBreedCatalog.OffersSitting(breed),
                IsLargeBreed = DogBreedCatalog.NeedsTrial(breed),
                CreatedAt = now,
                UpdatedAt = now
            });
            await db.SaveChangesAsync(cancellationToken);
            if (!DogBreedCatalog.OffersSitting(breed))
            {
                throw new InvalidOperationException(DogBreedCatalog.DeclinedMessage);
            }

            return breed;
        }

        if (!existing.OffersSitting)
        {
            throw new InvalidOperationException(DogBreedCatalog.DeclinedMessage);
        }

        if (existing.IsRejected)
        {
            existing.IsRejected = false;
            existing.IsApproved = false;
            existing.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }

        return existing.Name;
    }

    public async Task<bool> RequiresTrialAsync(string name, CancellationToken cancellationToken = default)
    {
        var breed = Normalize(name);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.DogBreeds.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Name == breed, cancellationToken);
        if (existing is not null)
        {
            return existing.OffersSitting && existing.IsLargeBreed;
        }

        return DogBreedCatalog.NeedsTrial(breed);
    }

    public async Task ApproveAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var breed = await db.DogBreeds.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("That breed was not found.");
        breed.IsApproved = true;
        breed.IsRejected = false;
        breed.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var breed = await db.DogBreeds.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("That breed was not found.");
        breed.IsApproved = false;
        breed.IsRejected = true;
        breed.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateAsync(string name, bool approved = true, bool? offersSitting = null, bool? isLargeBreed = null, CancellationToken cancellationToken = default)
    {
        var breed = Normalize(name);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        if (await db.DogBreeds.AnyAsync(b => b.Name == breed, cancellationToken))
        {
            throw new InvalidOperationException("That breed is already on the list.");
        }

        var now = DateTimeOffset.UtcNow;
        db.DogBreeds.Add(new DogBreed
        {
            Name = breed,
            IsApproved = approved,
            IsRejected = false,
            OffersSitting = offersSitting ?? DogBreedCatalog.OffersSitting(breed),
            IsLargeBreed = isLargeBreed ?? DogBreedCatalog.NeedsTrial(breed),
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(int id, string name, bool offersSitting, bool isLargeBreed, CancellationToken cancellationToken = default)
    {
        var breedName = Normalize(name);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var breed = await db.DogBreeds.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("That breed was not found.");
        if (await db.DogBreeds.AnyAsync(b => b.Id != id && b.Name == breedName, cancellationToken))
        {
            throw new InvalidOperationException("That breed is already on the list.");
        }

        breed.Name = breedName;
        breed.OffersSitting = offersSitting;
        breed.IsLargeBreed = isLargeBreed;
        breed.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var removed = await db.DogBreeds.Where(b => b.Id == id).ExecuteDeleteAsync(cancellationToken);
        if (removed == 0)
        {
            throw new InvalidOperationException("That breed was not found.");
        }
    }

    private static string Normalize(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length is < 2 or > 80)
        {
            throw new InvalidOperationException("Tell us the dog's breed.");
        }

        return string.Join(
            ' ',
            trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
