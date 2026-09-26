namespace AshaNandanvan.Application.DogSitting;

public sealed record DogSittingSettingsModel
{
    public int MaxDogs { get; set; } = 5;
    public string Headline { get; set; } = "Backyard dog sit";
    public string Description { get; set; } = string.Empty;
    public string TermsAndConditions { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
}

public sealed record StayAvailability(
    int MaxDogs,
    int ConfirmedDogs,
    int PendingDogs,
    int Remaining,
    bool CanBook,
    string Status,
    string Message,
    string? Warning);
