namespace AshaNandanvan.Application.Options;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    /// <summary>Google account email promoted to Admin on first sign-in.</summary>
    public string SeedEmail { get; set; } = string.Empty;
}
