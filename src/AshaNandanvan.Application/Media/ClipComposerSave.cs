namespace AshaNandanvan.Application.Media;

public sealed class ClipComposerSave
{
    public string SourceUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public DateTime? FilmedOn { get; set; }
    public string OfferSlug { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool FromUpload { get; set; }
}

public enum ClipComposerDestination
{
    Album,
    Media
}
