namespace AshaNandanvan.Domain.Entities;

public class StayRatePlan
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal FirstDogPerNight { get; set; }
    public decimal ExtraDogPerNight { get; set; }
    public int MinNights { get; set; }
    public int? MaxNights { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
}
