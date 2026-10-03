namespace AshaNandanvan.Application.Options;

public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Mixed into the IP hash so a database copy cannot be reversed back to addresses.
    /// Changing it restarts the "unique networks" count, so set it once and leave it.
    /// </summary>
    public string IpHashSalt { get; set; } = string.Empty;

    public int SessionTimeoutMinutes { get; set; } = 30;

    /// <summary>Page views and events older than this are deleted nightly. 0 keeps everything.</summary>
    public int RetentionDays { get; set; } = 180;
}
