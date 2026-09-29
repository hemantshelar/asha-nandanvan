using AshaNandanvan.Application.DogSitting;
using AshaNandanvan.Application.Offers;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Infrastructure.Data;
using AshaNandanvan.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Infrastructure.DogSitting;

public sealed class StayPlanService : IStayPlanService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public StayPlanService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<IReadOnlyList<StayRatePlanView>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsurePlansAsync(db, cancellationToken);
        var plans = await db.StayRatePlans.AsNoTracking().OrderBy(p => p.SortOrder).ToListAsync(cancellationToken);
        return plans.Select(ToView).ToList();
    }

    public async Task SaveRatesAsync(IReadOnlyList<StayRatePlanEdit> plans, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await EnsurePlansAsync(db, cancellationToken);
        foreach (var edit in plans)
        {
            if (edit.FirstDogPerNight < 0 || edit.ExtraDogPerNight < 0)
            {
                throw new InvalidOperationException("Plan rates cannot be negative.");
            }

            var plan = await db.StayRatePlans.FirstOrDefaultAsync(p => p.Id == edit.Id, cancellationToken)
                ?? throw new InvalidOperationException("That plan was not found.");
            plan.FirstDogPerNight = edit.FirstDogPerNight;
            plan.ExtraDogPerNight = edit.ExtraDogPerNight;
        }

        var regular = await db.StayRatePlans.FirstAsync(p => p.IsDefault, cancellationToken);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Slug == DogSittingService.ProductSlug, cancellationToken);
        if (product is not null)
        {
            product.Price = regular.FirstDogPerNight;
            product.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StayPlanQuote> QuoteAsync(
        string? userId,
        DateTimeOffset dropOff,
        DateTimeOffset pickUp,
        int dogs,
        bool trialStay = false,
        CancellationToken cancellationToken = default)
    {
        var plans = await ListAsync(cancellationToken);
        var nights = BookingPricing.NightCount(dropOff, pickUp);
        var pinnedId = string.IsNullOrWhiteSpace(userId)
            ? null
            : await GetAssignedPlanIdAsync(userId, cancellationToken);
        var pinned = pinnedId is int id && plans.Any(p => p.Id == id);
        var plan = pinned
            ? plans.First(p => p.Id == pinnedId)
            : StayPlanPicker.Auto(plans, nights);
        return StayPlanPicker.Quote(plan, nights, dogs, pinned, trialStay);
    }

    public async Task<int?> GetAssignedPlanIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.AssignedStayPlanId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static StayRatePlanView ToView(StayRatePlan plan) =>
        new(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.FirstDogPerNight,
            plan.ExtraDogPerNight,
            plan.MinNights,
            plan.MaxNights,
            plan.SortOrder,
            plan.IsDefault);

    public static async Task EnsurePlansAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.StayRatePlans.AnyAsync(cancellationToken))
        {
            return;
        }

        db.StayRatePlans.AddRange(
            new StayRatePlan { Code = "Regular", Name = "Regular", FirstDogPerNight = 55, ExtraDogPerNight = 25, MinNights = 1, MaxNights = 5, SortOrder = 1, IsDefault = true },
            new StayRatePlan { Code = "Standard", Name = "Standard", FirstDogPerNight = 55, ExtraDogPerNight = 20, MinNights = 6, MaxNights = 10, SortOrder = 2 },
            new StayRatePlan { Code = "Silver", Name = "Silver", FirstDogPerNight = 55, ExtraDogPerNight = 15, MinNights = 11, MaxNights = 15, SortOrder = 3 },
            new StayRatePlan { Code = "Gold", Name = "Gold", FirstDogPerNight = 55, ExtraDogPerNight = 10, MinNights = 16, MaxNights = null, SortOrder = 4 });
        await db.SaveChangesAsync(cancellationToken);
    }
}
