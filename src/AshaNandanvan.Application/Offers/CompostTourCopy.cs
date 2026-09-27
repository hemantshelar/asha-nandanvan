namespace AshaNandanvan.Application.Offers;

public sealed record TourAlbumItem(string Src, string Alt, bool IsVideoPlaceholder = false);

public sealed record TourStop(
    string Eyebrow,
    string Title,
    string Body,
    IReadOnlyList<TourAlbumItem> Album);

public static class CompostTourCopy
{
    public static IReadOnlyList<TourStop> Stops { get; } =
    [
        new(
            "Stop one",
            "Look around the worm farm",
            "We start at the bins, lids off. You will see the working face: kitchen scraps, garden trimmings, and a dark busy layer of compost worms. We talk about what they will eat, what they will not, and why finished castings smell sweet instead of rotten. You can hold a handful of black gold at different stages — fresh bedding, mid-bin, and the finished soil we put back on the beds.",
            [
                new("/images/tours/worm-farm-1.svg", "Placeholder — worm farm bins"),
                new("/images/tours/worm-farm-2.svg", "Placeholder — working face of the bin"),
                new("/images/tours/worm-farm-3.svg", "Placeholder — black gold in a hand"),
                new("/images/tours/worm-farm-4.svg", "Placeholder — lid off the bin"),
                new("/images/tours/worm-farm-5.svg", "Placeholder — scraps on the surface"),
                new("/images/tours/worm-farm-6.svg", "Placeholder — finished castings"),
                new("/images/tours/worm-farm-video.svg", "Placeholder — worm farm clip", true)
            ]),
        new(
            "Stop two",
            "Look around the chicken coop",
            "Next we walk to the hens. They finish what the worms cannot — tougher scraps, path scratchings, leftover greens. The coop sits in the same loop: manure is composted before it ever touches a salad bed, and eggs are the only thing that leaves the flock as food. It is a noisy, useful corner, not a show pen. You will see how the birds and the bins keep each other honest.",
            [
                new("/images/tours/chicken-coop-1.svg", "Placeholder — chicken coop"),
                new("/images/tours/chicken-coop-2.svg", "Placeholder — hens in the run"),
                new("/images/tours/chicken-coop-3.svg", "Placeholder — nest box"),
                new("/images/tours/chicken-coop-4.svg", "Placeholder — water and scratch"),
                new("/images/tours/chicken-coop-5.svg", "Placeholder — flock at the gate"),
                new("/images/tours/chicken-coop-6.svg", "Placeholder — composting manure"),
                new("/images/tours/chicken-coop-video.svg", "Placeholder — coop clip", true)
            ]),
        new(
            "Stop three",
            "Look around the mealworm farm",
            "Last stop is the mealworm trays. This is protein for the hens and another way scraps stay in the backyard. You will see the life cycle stacked in trays — beetles, larvae, frass — and how a small farm like this cuts bought-in feed. It is the quietest station, and usually the one people lean in closest to. By the time we leave, the whole loop is on the ground in front of you.",
            [
                new("/images/tours/mealworm-1.svg", "Placeholder — mealworm trays"),
                new("/images/tours/mealworm-2.svg", "Placeholder — larvae close up"),
                new("/images/tours/mealworm-3.svg", "Placeholder — stacked farm"),
                new("/images/tours/mealworm-4.svg", "Placeholder — beetles"),
                new("/images/tours/mealworm-5.svg", "Placeholder — frass"),
                new("/images/tours/mealworm-6.svg", "Placeholder — feed for the hens"),
                new("/images/tours/mealworm-video.svg", "Placeholder — mealworm clip", true)
            ])
    ];
}
