using AshaNandanvan.Application.Common;
using AshaNandanvan.Application.Offers;
using AshaNandanvan.Infrastructure.DogSitting;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Infrastructure.Data;

public sealed class DatabaseSeeder
{
    private readonly AppDbContext _db;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DatabaseSeeder(AppDbContext db, RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var role in new[] { AppRoles.Admin, AppRoles.Customer })
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var now = DateTimeOffset.UtcNow;
        if (!await _db.Products.AnyAsync(cancellationToken))
        {
            _db.Products.AddRange(
            new Product
            {
                Name = "Backyard eggs",
                Slug = "backyard-eggs",
                Description = "A dozen eggs from our Sydney backyard flock. The hens forage among the beds, and the yolks are deep gold.",
                Category = ProductCategory.Eggs,
                Price = 9.50m,
                Stock = 18,
                Unit = "dozen",
                ImagePath = "/images/eggs.svg",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                Name = "Seasonal leafy greens",
                Slug = "seasonal-leafy-greens",
                Description = "A mixed bunch harvested the same morning — whatever is happiest in the beds that week.",
                Category = ProductCategory.Veggies,
                Price = 6.00m,
                Stock = 24,
                Unit = "bunch",
                ImagePath = "/images/greens.svg",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                Name = "Garden tomatoes",
                Slug = "garden-tomatoes",
                Description = "Sun-ripened tomatoes from the backyard. Flavour first, never forced.",
                Category = ProductCategory.Veggies,
                Price = 7.50m,
                Stock = 16,
                Unit = "punnet",
                ImagePath = "/images/tomatoes.svg",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                Name = "Live compost worms",
                Slug = "live-compost-worms",
                Description = "A starter colony of compost worms ready to turn your scraps into soil. We raise them here in Sydney.",
                Category = ProductCategory.WormsAndCompost,
                Price = 18.00m,
                Stock = 12,
                Unit = "punnet",
                ImagePath = "/images/worms.svg",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                Name = "Black gold compost",
                Slug = "black-gold-compost",
                Description = "Finished worm castings — the black gold our garden runs on. What grows here stays here, and this is how we close the loop.",
                Category = ProductCategory.WormsAndCompost,
                Price = 12.00m,
                Stock = 20,
                Unit = "2 kg bag",
                ImagePath = "/images/compost.svg",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        await EnsureServiceProductsAsync(now, cancellationToken);
    }

    private async Task EnsureServiceProductsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var dogSit = await _db.Products.FirstOrDefaultAsync(p => p.Slug == "backyard-dog-sit", cancellationToken);
        if (dogSit is null)
        {
            dogSit = new Product
            {
                Name = "Backyard dog sit",
                Slug = "backyard-dog-sit",
                Description = "Your dog stays with us in the Sydney backyard — hens, garden, and a quiet run. One dog per stay. Choose an open window, then we confirm drop-off after payment.",
                Category = ProductCategory.DogSitting,
                Price = 55.00m,
                Stock = 0,
                Unit = "night",
                ImagePath = OfferCatalog.DogSitting.ImagePath,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Products.Add(dogSit);
        }
        else if (dogSit.ImagePath != OfferCatalog.DogSitting.ImagePath)
        {
            dogSit.ImagePath = OfferCatalog.DogSitting.ImagePath;
            dogSit.UpdatedAt = now;
        }

        if (!await _db.Products.AnyAsync(p => p.Slug == "composting-education-tour", cancellationToken))
        {
            _db.Products.Add(new Product
            {
                Name = "Composting education tour",
                Slug = "composting-education-tour",
                Description = "A small-group walk through the composting loop: scraps, worms, castings, and the beds they feed. About 90 minutes at the backyard.",
                Category = ProductCategory.CompostTour,
                Price = 25.00m,
                Stock = 0,
                Unit = "guest",
                ImagePath = "/images/compost.svg",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (!await _db.DogSittingSettings.AnyAsync(cancellationToken))
        {
            _db.DogSittingSettings.Add(new DogSittingSettings
            {
                MaxDogs = 5,
                Headline = dogSit.Name,
                Description = dogSit.Description,
                TermsAndConditions = DogSittingService.DefaultTerms,
                UpdatedAt = now
            });
        }

        var tour = await _db.Products.FirstAsync(p => p.Slug == "composting-education-tour", cancellationToken);

        if (!await _db.ProductSlots.AnyAsync(s => s.ProductId == tour.Id, cancellationToken))
        {
            var saturday = NextWeekday(DateTime.Today, DayOfWeek.Saturday);
            _db.ProductSlots.AddRange(
                Session(tour.Id, saturday, 10),
                Session(tour.Id, saturday.AddDays(7), 10),
                Session(tour.Id, saturday.AddDays(14), 14));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static DateTime NextWeekday(DateTime from, DayOfWeek day)
    {
        var delta = ((int)day - (int)from.DayOfWeek + 7) % 7;
        return from.AddDays(delta == 0 ? 7 : delta);
    }

    private static ProductSlot Session(int productId, DateTime date, int hour)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "AUS Eastern Standard Time" : "Australia/Sydney");
        var local = date.Date.AddHours(hour);
        var start = new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), tz.GetUtcOffset(local));
        return new ProductSlot
        {
            ProductId = productId,
            StartsAt = start,
            EndsAt = start.AddHours(1.5),
            Capacity = 8,
            BookedCount = 0,
            IsActive = true
        };
    }
}
