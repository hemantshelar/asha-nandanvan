using System.ComponentModel.DataAnnotations.Schema;

namespace AshaNandanvan.Domain.Entities;

public class ProductSlot
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int Capacity { get; set; } = 1;
    public int BookedCount { get; set; }
    public string? Label { get; set; }
    public bool IsActive { get; set; } = true;

    [NotMapped]
    public int Remaining => Math.Max(0, Capacity - BookedCount);
}
