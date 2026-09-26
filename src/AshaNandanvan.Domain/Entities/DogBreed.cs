namespace AshaNandanvan.Domain.Entities;

public class DogBreed
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public bool IsRejected { get; set; }
    public bool OffersSitting { get; set; } = true;
    public bool IsLargeBreed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
