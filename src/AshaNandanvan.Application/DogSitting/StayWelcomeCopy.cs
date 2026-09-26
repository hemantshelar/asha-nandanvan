namespace AshaNandanvan.Application.DogSitting;

public sealed record StayWelcomeLetter(
    string Activity,
    string Flock,
    string ClipCaption,
    string BringHeading,
    IReadOnlyList<string> Bring,
    string ShareHeading,
    IReadOnlyList<string> Share,
    string AfterHeading,
    IReadOnlyList<string> After,
    string Close);

public static class StayWelcomeCopy
{
    public const string AshaClipId = "yOZO0b_xhGg";

    public static string DisplayName(string? petName) =>
        string.IsNullOrWhiteSpace(petName) ? "your dog" : petName.Trim();

    public static StayWelcomeLetter Build(string? petName)
    {
        var name = DisplayName(petName);
        var possessive = string.IsNullOrWhiteSpace(petName) ? "your dog's" : $"{petName.Trim()}'s";

        return new StayWelcomeLetter(
            $"{Sentence(name)} stays in a real Sydney backyard, not a kennel. There is room to run, a 1-year-old Border Collie — Asha — for company, and a small flock of free-range hens.",
            $"The hens give visiting dogs a job: watch, follow, stay alert. That keeps {name} engaged, busy, and properly exercised for the whole stay — not waiting at the fence. We introduce {name} to the flock carefully, and supervise if they need it.",
            "Asha working the backyard flock. This is the kind of day a stay looks like.",
            "What to pack",
            [
                "Their usual food",
                "Their own bowl",
                "A bed, blanket, or anything that smells like home"
            ],
            "Send us before drop-off",
            [
                "Pet insurance details, if they have cover",
                $"{Sentence(possessive)} usual vet — clinic name, address, and phone",
                "A second emergency contact — name, mobile, and email"
            ],
            "Once you book",
            [
                "We confirm drop-off and pick-up times",
                $"Tell us anything we should know about {possessive} routine, feeding, or behaviour"
            ],
            $"We look after {name} so the stay is safe, active, and actually fun.");
    }

    private static string Sentence(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
