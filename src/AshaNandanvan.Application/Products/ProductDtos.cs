using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Application.Products;

public sealed record ProductListItem(
    int Id,
    string Name,
    string Slug,
    string Description,
    ProductCategory Category,
    decimal Price,
    int Stock,
    string Unit,
    string? ImagePath,
    bool IsActive)
{
    public bool RequiresBooking => Category.RequiresBooking();
}

public sealed record ProductSlotItem(
    int Id,
    int ProductId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int Capacity,
    int BookedCount,
    int Remaining,
    string Label,
    bool IsActive);

public sealed record ProductSlotEditModel
{
    public int ProductId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int Capacity { get; set; } = 1;
    public string? Label { get; set; }
}

public sealed record ProductEditModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProductCategory Category { get; set; } = ProductCategory.Veggies;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Unit { get; set; } = "each";
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;
}
