namespace AshaNandanvan.Domain.Entities;

public class CartItem
{
    public int Id { get; set; }
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductSlotId { get; set; }
    public ProductSlot? ProductSlot { get; set; }
    public DateTimeOffset? StayStartsAt { get; set; }
    public DateTimeOffset? StayEndsAt { get; set; }
    public string? PetName { get; set; }
    public string? PetBreed { get; set; }
    public bool IsTrialStay { get; set; }
    public DateTimeOffset? IntendedStayStartsAt { get; set; }
    public DateTimeOffset? IntendedStayEndsAt { get; set; }
    public int Quantity { get; set; }
}
