using AshaNandanvan.Application.Common;
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

        if (await _db.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
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
}
