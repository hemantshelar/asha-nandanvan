using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Domain.Entities;

/// <summary>
/// One continuous visit. VisitorId lives as long as the browser keeps its cookie, so it
/// identifies a device rather than a person; IpHash is kept only to count distinct networks.
/// </summary>
public class VisitSession
{
    public int Id { get; set; }
    public Guid VisitorId { get; set; }
    public Guid SessionKey { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public int PageViews { get; set; }
    public bool IsFirstVisit { get; set; }

    public string? UserId { get; set; }

    public string Source { get; set; } = string.Empty;
    public string FirstTouchSource { get; set; } = string.Empty;
    public string? Campaign { get; set; }
    public string? Medium { get; set; }
    public string? Referrer { get; set; }
    public string LandingPath { get; set; } = string.Empty;
    public string? ExitPath { get; set; }

    public string IpHash { get; set; } = string.Empty;
    public string? IpNetwork { get; set; }
    public string? Country { get; set; }

    public VisitDevice Device { get; set; }
    public string? Browser { get; set; }
    public string? BrowserVersion { get; set; }
    public string? Platform { get; set; }
    public string? PlatformVersion { get; set; }
    public string? DeviceModel { get; set; }
    public int? ViewportWidth { get; set; }
    public string? UserAgent { get; set; }
    public bool IsBot { get; set; }

    public ICollection<VisitEvent> Events { get; set; } = [];

    public int SecondsOnSite => (int)Math.Max(0, (LastSeenAt - StartedAt).TotalSeconds);
}
