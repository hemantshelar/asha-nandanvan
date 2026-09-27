namespace AshaNandanvan.Application.Offers;

public sealed record TourAlbumItem(
    string Src,
    string Alt,
    bool IsVideoPlaceholder = false,
    bool IsVideo = false,
    string? VideoSrc = null,
    string? FullSrc = null,
    string? Title = null,
    string? Caption = null)
{
    public string LightboxSrc => FullSrc ?? Src;
    public string Heading => Title ?? Alt;
}

public sealed record TourStop(
    string Eyebrow,
    string Title,
    IReadOnlyList<string> Paragraphs,
    IReadOnlyList<TourAlbumItem> Album,
    string? AlbumCaption = null);

public static class CompostTourCopy
{
    public static IReadOnlyList<TourStop> Stops { get; } =
    [
        new(
            "Stop one",
            "Look around the worm farm",
            [
                "We start at the bins, lids off. You will see the working face: kitchen scraps, garden trimmings, and a dark busy layer of compost worms. We talk about what they will eat, what they will not, and why finished castings smell sweet instead of rotten. You can hold a handful of black gold at different stages — fresh bedding, mid-bin, and the finished soil we put back on the beds."
            ],
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
            [
                "Next we walk to the hens. They finish what the worms cannot — tougher scraps, path scratchings, leftover greens. The coop sits in the same loop: manure is composted before it ever touches a salad bed, and eggs are the only thing that leaves the flock as food. It is a noisy, useful corner, not a show pen. You will see how the birds and the bins keep each other honest."
            ],
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
            [
                "Last stop is the quietest, and the one people lean in closest to. These are yellow mealworms — the larvae of the darkling beetle. They turn leftover oats, carrot ends, and peel into live protein for the hens, so we buy less bagged feed.",
                "The whole cycle is in the trays. A creamy larva eats, then folds into a still pupa. A few days later a pale, soft beetle climbs out. It darkens to chestnut, then almost black. Those adults lay the next generation in the same bedding.",
                "What they leave behind is frass — a fine dry powder that looks like bran. It is not waste. We sift it from the oats and put it on the garden, or mix a pinch into hen feed. Larva, pupa, beetle, powder: nothing in this corner is rubbish."
            ],
            CreateMealwormAlbum(),
            "The life cycle — from hungry larva to dark beetle, and the frass they leave behind. Tap a photo or clip to open it.")
    ];

    private static IReadOnlyList<TourAlbumItem> CreateMealwormAlbum() =>
    [
        Photo("larvae-oats", "Larvae in oats and carrot", "The working farm",
            "Mealworms live in dry oats. Carrot sticks are their water — they drink from the veg, so the bedding stays dry and does not rot."),
        Video("larvae-move", "Mealworms moving through oats", "Watch them work",
            "Lift a handful and the tray comes alive. This is the protein that later walks over to the chicken coop."),
        Photo("mixed-bucket", "Larvae and beetles sharing a bucket", "All ages in one bucket",
            "You will often see larvae and a few dark beetles together. The farm is not a neat classroom diagram — generations overlap."),
        Photo("larva-close", "A mealworm larva on orange peel", "The mealworm",
            "This is the larva people mean when they say mealworm. Soft, segmented, always eating. Hens take them whole."),
        Photo("pupa", "A mealworm pupa on orange peel", "The still stage",
            "When a larva is ready, it curls into a pupa and goes quiet. No eating. Inside, the beetle is being built."),
        Photo("beetles-new", "Newly emerged pale beetles in a hand", "Just hatched",
            "A new beetle is pale and a little wobbly. The shell is still soft. Give it a day or two and the colour starts to come in."),
        Photo("beetle-soft", "A soft new beetle on orange peel", "Soft shell",
            "Hold one on a scrap of peel and you can see the ridges before they harden. This is the same animal that was a worm last week."),
        Photo("beetles-card", "Young beetles on cardboard", "Chestnut coats",
            "We lift beetles onto card so you can count them. Pale cream first, then tan, then the dark armour of an adult."),
        Photo("beetle-among", "A tan beetle among larvae and oats", "Back in the tray",
            "Adults live in the same oats as the young. They lay eggs in the bedding. The next larvae hatch where their parents are still walking."),
        Video("tray-hide", "Trays with cardboard hide and carrots", "Hide and moisture",
            "A scrap of card gives beetles a roof. Carrot keeps them hydrated. Simple kit — buckets, oats, and kitchen leftovers."),
        Photo("beetles-dark", "Dark adult beetles in a sieve", "Grown up",
            "Mature darkling beetles are almost black. We sieve them when we want to show the adult stage, or to move a breeding group."),
        Photo("beetle-dark-close", "Close-up of a dark mealworm beetle", "The darkling beetle",
            "This is Tenebrio molitor, the beetle at the end of the line. From here the story starts again — eggs in the oats, then larvae."),
        Photo("frass", "Sifted mealworm frass in a blue bowl", "Frass — the byproduct",
            "Frass is the fine powder left after the worms have worked the oats. We sift it out. On the garden it is a gentle fertiliser. A little also goes back to the hens.")
    ];

    private static TourAlbumItem Photo(string name, string alt, string title, string caption) =>
        new($"/images/tours/mealworms/{name}.jpg", alt,
            FullSrc: $"/images/tours/mealworms/{name}-full.jpg",
            Title: title,
            Caption: caption);

    private static TourAlbumItem Video(string name, string alt, string title, string caption) =>
        new($"/images/tours/mealworms/{name}.jpg", alt,
            IsVideo: true,
            VideoSrc: $"/images/tours/mealworms/{name}.mp4",
            Title: title,
            Caption: caption);
}
