using AshaNandanvan.Application.DogSitting;
using AshaNandanvan.Application.Offers;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Domain.Enums;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Infrastructure.DogSitting;

public sealed class DogSittingService : IDogSittingService
{
    public const string ProductSlug = "backyard-dog-sit";

    private static readonly OrderStatus[] ConfirmedStatuses =
    [
        OrderStatus.Placed,
        OrderStatus.Paid,
        OrderStatus.ReadyForPickup
    ];

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DogSittingService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<DogSittingSettingsModel> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var settings = await EnsureSettingsAsync(db, cancellationToken);
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == ProductSlug, cancellationToken);
        return new DogSittingSettingsModel
        {
            MaxDogs = settings.MaxDogs,
            Headline = settings.Headline,
            Description = settings.Description,
            TermsAndConditions = settings.TermsAndConditions,
            PricePerNight = product?.Price ?? 55m
        };
    }

    public async Task SaveSettingsAsync(DogSittingSettingsModel model, CancellationToken cancellationToken = default)
    {
        if (model.MaxDogs < 1)
        {
            throw new InvalidOperationException("Maximum dogs must be at least 1.");
        }

        if (string.IsNullOrWhiteSpace(model.Headline))
        {
            throw new InvalidOperationException("Give the stay a headline.");
        }

        if (string.IsNullOrWhiteSpace(model.Description))
        {
            throw new InvalidOperationException("Add a description visitors will read.");
        }

        if (string.IsNullOrWhiteSpace(model.TermsAndConditions))
        {
            throw new InvalidOperationException("Add terms and conditions.");
        }

        if (model.PricePerNight < 0)
        {
            throw new InvalidOperationException("Price cannot be negative.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var settings = await EnsureSettingsAsync(db, cancellationToken);
        settings.MaxDogs = model.MaxDogs;
        settings.Headline = model.Headline.Trim();
        settings.Description = model.Description.Trim();
        settings.TermsAndConditions = model.TermsAndConditions.Trim();
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        var product = await db.Products.FirstOrDefaultAsync(p => p.Slug == ProductSlug, cancellationToken);
        if (product is not null)
        {
            product.Name = settings.Headline;
            product.Description = settings.Description;
            product.Price = model.PricePerNight;
            product.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StayAvailability> CheckAvailabilityAsync(
        DateTimeOffset dropOff,
        DateTimeOffset pickUp,
        int dogs = 1,
        CancellationToken cancellationToken = default)
    {
        if (pickUp <= dropOff)
        {
            throw new InvalidOperationException("Pick-up must be after drop-off.");
        }

        if (dropOff < DateTimeOffset.UtcNow.AddMinutes(-5))
        {
            throw new InvalidOperationException("Drop-off cannot be in the past.");
        }

        if (dogs < 1)
        {
            throw new InvalidOperationException("Book at least one dog.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var settings = await EnsureSettingsAsync(db, cancellationToken);
        var stays = await LoadActiveStaysAsync(db, cancellationToken);

        var confirmed = StayOccupancy.MaxConcurrent(
            stays.Where(s => s.Confirmed).Select(s => (s.Start, s.End, s.Quantity)),
            dropOff,
            pickUp);
        var pending = StayOccupancy.MaxConcurrent(
            stays.Where(s => !s.Confirmed).Select(s => (s.Start, s.End, s.Quantity)),
            dropOff,
            pickUp);
        var remaining = Math.Max(0, settings.MaxDogs - confirmed);
        var canBook = confirmed + dogs <= settings.MaxDogs;

        var status = canBook
            ? confirmed == 0 ? "Open" : "Places left"
            : "Fully booked";

        var message = canBook
            ? $"{confirmed} of {settings.MaxDogs} dogs are already confirmed for those dates. {remaining} place{(remaining == 1 ? "" : "s")} left."
            : $"Those dates are fully booked. We already have {confirmed} confirmed dog{(confirmed == 1 ? "" : "s")} (maximum {settings.MaxDogs}).";

        string? warning = null;
        if (canBook && confirmed + pending + dogs > settings.MaxDogs)
        {
            warning = $"{pending} more dog{(pending == 1 ? " is" : "s are")} held on payment-started bookings. A placed or paid stay keeps the place.";
        }
        else if (!canBook && pending > 0)
        {
            warning = $"{pending} pending booking{(pending == 1 ? "" : "s")} also overlap these dates.";
        }

        return new StayAvailability(settings.MaxDogs, confirmed, pending, remaining, canBook, status, message, warning);
    }

    private static async Task<DogSittingSettings> EnsureSettingsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var settings = await db.DogSittingSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new DogSittingSettings
        {
            MaxDogs = 5,
            Headline = "Backyard dog sit",
            Description = "Your dog stays with us in the Sydney backyard — hens, garden, and a quiet run. Choose drop-off and pick-up, then we confirm after payment.",
            TermsAndConditions = DefaultTerms,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.DogSittingSettings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static async Task<List<ActiveStay>> LoadActiveStaysAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var rows = await db.OrderItems
            .AsNoTracking()
            .Include(i => i.Order)
            .Include(i => i.Product)
            .Include(i => i.ProductSlot)
            .Where(i => i.Product.Category == ProductCategory.DogSitting
                && i.Order.Status != OrderStatus.Cancelled
                && i.Order.Status != OrderStatus.Completed)
            .ToListAsync(cancellationToken);

        return rows
            .Select(i =>
            {
                var start = i.StayStartsAt ?? i.ProductSlot?.StartsAt;
                var end = i.StayEndsAt ?? i.ProductSlot?.EndsAt;
                if (start is null || end is null)
                {
                    return null;
                }

                return new ActiveStay(start.Value, end.Value, i.Quantity, ConfirmedStatuses.Contains(i.Order.Status));
            })
            .Where(s => s is not null)
            .Select(s => s!)
            .ToList();
    }

    public const string DefaultTerms =
        "One household per stay. Your dog must be vaccinated, flea-treated, and used to hens and a garden. "
        + "Drop-off and pick-up are at the backyard. Placing the order holds the place; you can pay now or after the stay, in cash or online. "
        + "If five dogs are already confirmed for any part of your dates, we cannot take another. "
        + "Food, leads, and any medicine must come with the dog. We are a backyard, not a clinic.";

    private sealed record ActiveStay(DateTimeOffset Start, DateTimeOffset End, int Quantity, bool Confirmed);
}
