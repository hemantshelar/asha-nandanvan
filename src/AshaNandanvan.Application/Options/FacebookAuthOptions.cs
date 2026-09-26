namespace AshaNandanvan.Application.Options;

public sealed class FacebookAuthOptions
{
    public const string SectionName = "FacebookAuth";

    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(AppSecret);
}
