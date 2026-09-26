namespace AshaNandanvan.Domain.Entities;

public class DogSittingSettings
{
    public int Id { get; set; }
    public int MaxDogs { get; set; } = 5;
    public string Headline { get; set; } = "Backyard dog sit";
    public string Description { get; set; } = string.Empty;
    public string TermsAndConditions { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
