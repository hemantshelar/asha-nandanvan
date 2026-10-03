using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Domain.Entities;

public class VisitEvent
{
    public long Id { get; set; }
    public int VisitSessionId { get; set; }
    public VisitSession Session { get; set; } = null!;

    public VisitEventKind Kind { get; set; }
    public DateTimeOffset At { get; set; }
    public string Path { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public decimal? Value { get; set; }
}
