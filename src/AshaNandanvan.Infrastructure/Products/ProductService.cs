using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AshaNandanvan.Application.Products;
using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Domain.Enums;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AshaNandanvan.Infrastructure.Products;

public sealed class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductListItem>> GetActiveAsync(ProductCategory? category = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Products.AsNoTracking().Where(p => p.IsActive);
        if (category is not null)
        {
            query = query.Where(p => p.Category == category);
        }

        var products = await query.OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync(cancellationToken);
        return products.Select(ToListItem).ToList();
    }

    public async Task<IReadOnlyList<ProductListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _db.Products.AsNoTracking()
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
        return products.Select(ToListItem).ToList();
    }

    public async Task<ProductListItem?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
        return product is null ? null : ToListItem(product);
    }

    public async Task<ProductListItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return product is null ? null : ToListItem(product);
    }

    public async Task<int> CreateAsync(ProductEditModel model, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            Name = model.Name.Trim(),
            Slug = await UniqueSlugAsync(model.Name, null, cancellationToken),
            Description = model.Description.Trim(),
            Category = model.Category,
            Price = model.Price,
            Stock = model.Stock,
            Unit = model.Unit.Trim(),
            ImagePath = model.ImagePath,
            IsActive = model.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    public async Task UpdateAsync(ProductEditModel model, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == model.Id, cancellationToken)
            ?? throw new InvalidOperationException("Product was not found.");

        product.Name = model.Name.Trim();
        product.Slug = await UniqueSlugAsync(model.Name, product.Id, cancellationToken);
        product.Description = model.Description.Trim();
        product.Category = model.Category;
        product.Price = model.Price;
        product.Stock = model.Stock;
        product.Unit = model.Unit.Trim();
        product.ImagePath = model.ImagePath;
        product.IsActive = model.IsActive;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> UniqueSlugAsync(string name, int? existingId, CancellationToken cancellationToken)
    {
        var slug = Slugify(name);
        var candidate = slug;
        var suffix = 2;
        while (await _db.Products.AnyAsync(p => p.Slug == candidate && p.Id != existingId, cancellationToken))
        {
            candidate = $"{slug}-{suffix++}";
        }

        return candidate;
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        var slug = Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "product" : slug;
    }

    private static ProductListItem ToListItem(Product p) =>
        new(p.Id, p.Name, p.Slug, p.Description, p.Category, p.Price, p.Stock, p.Unit, p.ImagePath, p.IsActive);
}
