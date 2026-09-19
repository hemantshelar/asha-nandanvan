namespace AshaNandanvan.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
