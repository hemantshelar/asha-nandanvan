namespace AshaNandanvan.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int? ProductSlotId { get; set; }
    public ProductSlot? ProductSlot { get; set; }
    public DateTimeOffset? StayStartsAt { get; set; }
    public DateTimeOffset? StayEndsAt { get; set; }
    public string? PetName { get; set; }
    public string? PetBreed { get; set; }
    public bool IsTrialStay { get; set; }
    public DateTimeOffset? IntendedStayStartsAt { get; set; }
    public DateTimeOffset? IntendedStayEndsAt { get; set; }
    public string? SlotLabel { get; set; }
}
