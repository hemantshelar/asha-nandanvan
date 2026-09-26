using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProductCategory Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<ProductSlot> Slots { get; set; } = [];
}
