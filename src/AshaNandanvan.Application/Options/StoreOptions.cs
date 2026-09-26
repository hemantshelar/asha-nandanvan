using System.ComponentModel.DataAnnotations;

namespace AshaNandanvan.Application.Options;

public sealed class StoreOptions
{
    public const string SectionName = "Store";

    [Required]
    public string Name { get; set; } = "Asha Nandanvan";

    [Required]
    public string Tagline { get; set; } = "What grows in our backyard stays in our backyard.";

    [Required]
    public string City { get; set; } = "Sydney";

    [Required]
    public string PickupAddress { get; set; } = "Sydney, NSW — exact backyard address shared after your order is paid.";

    [Required]
    public string PickupHours { get; set; } = "Saturday and Sunday, 9am–12pm";

    public List<string> PickupWindows { get; set; } = ["Saturday 9am–12pm", "Sunday 9am–12pm"];

    [Required]
    [Url]
    public string FacebookUrl { get; set; } = "https://www.facebook.com/profile.php?id=61587497242988";

    [Required]
    [Url]
    public string InstagramUrl { get; set; } = "https://www.instagram.com/ashanandanvan/";

    [Required]
    [Url]
    public string YouTubeUrl { get; set; } = "https://www.youtube.com/@ashanandanvan";

    [Required]
    public string Currency { get; set; } = "AUD";

    public string CurrencySymbol { get; set; } = "$";
}
