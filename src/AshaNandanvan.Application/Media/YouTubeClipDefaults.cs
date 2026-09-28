using AshaNandanvan.Application.Offers;

namespace AshaNandanvan.Application.Media;

public static class YouTubeClipDefaults
{
    public static string AlbumTitle(string petName) =>
        string.IsNullOrWhiteSpace(petName) ? "Stay clip" : petName.Trim();

    public static string AlbumDescription(string stayLabel) =>
        string.IsNullOrWhiteSpace(stayLabel)
            ? "Private stay clip — not for the public channel."
            : $"{stayLabel.Trim()}. Private stay clip — not for the public channel.";

    public static IReadOnlyList<string> AlbumTags(string petName)
    {
        var tags = new List<string> { "asha nandanvan", "dog sitting" };
        if (!string.IsNullOrWhiteSpace(petName))
        {
            tags.Insert(0, petName.Trim());
        }

        return tags;
    }

    public static string MediaTitle(OfferDefinition offer) => $"{offer.Title} clip";

    public static string MediaDescription(OfferDefinition offer) => offer.Summary;

    public static IReadOnlyList<string> MediaTags(OfferDefinition offer) =>
        [offer.Eyebrow, "asha nandanvan"];

    public static string PrivacyLabel(YouTubePrivacy privacy) =>
        privacy == YouTubePrivacy.Public
            ? "Public on the Media page and your channel"
            : "Unlisted — only people in this album";
}
