namespace AshaNandanvan.Application.DogSitting;

public sealed record StayWelcomeLetter(
    string Intro,
    string BringHeading,
    IReadOnlyList<string> Bring,
    string ShareHeading,
    IReadOnlyList<string> Share,
    string GroupHeading,
    IReadOnlyList<string> Group,
    string Close);

public static class StayWelcomeCopy
{
    public static string DisplayName(string? petName) =>
        string.IsNullOrWhiteSpace(petName) ? "your dog" : petName.Trim();

    public static StayWelcomeLetter Build(string? petName)
    {
        var name = DisplayName(petName);
        var possessive = string.IsNullOrWhiteSpace(petName) ? "your dog's" : $"{petName.Trim()}'s";

        return new StayWelcomeLetter(
            $"{Sentence(name)} will have access to a spacious backyard to run and play, plus company from my friendly 1-year-old border collie. This will be a great socialization opportunity, especially for active and sociable dogs. As mentioned in the ad, I also have a few chickens in the backyard, so we’ll make sure {name} is comfortable around them or appropriately supervised.",
            $"For {possessive} comfort and hygiene, please bring:",
            [
                "Regular food",
                "Own food bowl",
                "Any bed, blanket, or comfort items you’d like them to have"
            ],
            "Before the stay, could you please share:",
            [
                $"Any pet insurance details (if {name} is covered)",
                $"{Sentence(possessive)} preferred vet/doctor (clinic name, address, and phone)",
                "An alternative emergency contact (name, mobile number, and email)"
            ],
            "We can use this group for:",
            [
                "Sharing drop-off and pick-up times",
                $"Any questions or special instructions about {possessive} routine, feeding, or behaviour"
            ],
            $"Looking forward to having {name} stay over and making sure they have a safe, happy, and fun time!");
    }

    private static string Sentence(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
