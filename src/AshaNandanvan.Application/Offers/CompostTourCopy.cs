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
                "We start with a bucket, lid off. Kitchen peelings, grass clippings, and garden waste come here first. Dry browns on top, greens tucked under, worms working out of the sun. This is the whole loop at teaching size — you can see every stage in one pot.",
                "Lift the lid and you see the working face. Fresh scraps sit on top — pumpkin, banana skin, egg shell, cucumber ends. Underneath, compost worms pull the food down and leave dark castings. We talk about what they will eat (peels, coffee, grass, wilted greens) and what they will not (oily leftovers, too much onion). It should smell sweet and earthy, never rotten.",
                "Weeks later the scraps are gone. What is left is black gold — crumbly, dark, and ready for the beds. Then we walk to the high-rise: the same farm, just bigger. Same worms, same scraps, same gold. That soil goes back under the tomatoes. The loop is closed: kitchen to worms to garden to kitchen again."
            ],
            CreateEarthwormAlbum(),
            "Kitchen scraps and garden waste become black gold in a bucket. Last come the photo and clip of the high-rise bed — the bigger version of the same loop. Tap a photo or clip to open it."),
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

    private static IReadOnlyList<TourAlbumItem> CreateEarthwormAlbum() =>
    [
        Photo("earthworms", "kitchen-bag", "Kitchen scraps in a compostable bag", "What leaves the kitchen",
            "Peelings, cucumber ends, onion skins — this is the bag that would have gone in the bin. It comes to the worms instead."),
        Photo("earthworms", "garden-waste", "Garden stems and clippings in a pot", "Grass and garden waste",
            "Not only peelings. Wilted stems, clippings, and dry stalks go in as browns. They keep the bin from turning wet and sour."),
        Photo("earthworms", "working-face", "Worms in pumpkin scraps and peel", "The working face",
            "Lid off. Fresh pumpkin and peel on top, worms already in the food. This is the layer we talk about on the tour."),
        Video("earthworms", "fresh-feed", "Banana, egg shell and greens in a bucket", "Dinner for the worms",
            "A banana, an egg shell, cucumber, a few leaves. We bury this under a handful of bedding so flies stay away and the worms find it."),
        Video("earthworms", "worms-eat", "Compost worms moving through kitchen scraps", "They pull it down",
            "Watch them work. They do not chew a pile from the top — they drag scraps into the dark and leave castings behind."),
        Photo("earthworms", "mid-bin", "Worms in a dark mid-bin mix", "Mid-bin",
            "A week or two later the scraps have lost their shape. The mix is dark, damp, and busy. This is the middle of the story."),
        Video("earthworms", "bucket-work", "Worms and leftover scraps in a bucket", "The bucket farm",
            "The small farm is honest: you can see every stage in one pot. Same worms, same scraps, same gold — just less of it."),
        Photo("earthworms", "almost-gold", "Dark worm castings still holding a few worms", "Almost gold",
            "The last scraps are gone. What is left is nearly soil. A few worms stay until we harvest, then we move them to the next feed."),
        Video("earthworms", "gold-close", "Finished crumbly worm castings", "Crumbly and dark",
            "Finished castings should smell sweet, not rotten. Rub a pinch — it is fine, dark, and ready for the beds."),
        Photo("earthworms", "black-gold", "Harvested worm castings in a pot", "Black gold",
            "This is what we put back on the garden. Kitchen waste, grass, and stems — turned, by worms, into soil we can hold."),
        Photo("earthworms", "highrise-bed", "High-rise worm bed with a bucket sitting in it", "The bigger version",
            "Same work as the bucket — just more of it. The long white tub is the high-rise bed: dry sawdust on top, worms underneath. The bucket sitting in it is the small farm you just walked through."),
        Video("earthworms", "highrise-walk", "Walking the high-rise worm bed", "A bucket that grew up",
            "Walk the rim and you see the scale. This is a bigger version of what we are doing in the bucket: more surface, more worms, more kitchen waste, same black gold.")
    ];

    private static IReadOnlyList<TourAlbumItem> CreateMealwormAlbum() =>
    [
        Photo("mealworms", "larvae-oats", "Larvae in oats and carrot", "The working farm",
            "Mealworms live in dry oats. Carrot sticks are their water — they drink from the veg, so the bedding stays dry and does not rot."),
        Video("mealworms", "larvae-move", "Mealworms moving through oats", "Watch them work",
            "Lift a handful and the tray comes alive. This is the protein that later walks over to the chicken coop."),
        Photo("mealworms", "mixed-bucket", "Larvae and beetles sharing a bucket", "All ages in one bucket",
            "You will often see larvae and a few dark beetles together. The farm is not a neat classroom diagram — generations overlap."),
        Photo("mealworms", "larva-close", "A mealworm larva on orange peel", "The mealworm",
            "This is the larva people mean when they say mealworm. Soft, segmented, always eating. Hens take them whole."),
        Photo("mealworms", "pupa", "A mealworm pupa on orange peel", "The still stage",
            "When a larva is ready, it curls into a pupa and goes quiet. No eating. Inside, the beetle is being built."),
        Photo("mealworms", "beetles-new", "Newly emerged pale beetles in a hand", "Just hatched",
            "A new beetle is pale and a little wobbly. The shell is still soft. Give it a day or two and the colour starts to come in."),
        Photo("mealworms", "beetle-soft", "A soft new beetle on orange peel", "Soft shell",
            "Hold one on a scrap of peel and you can see the ridges before they harden. This is the same animal that was a worm last week."),
        Photo("mealworms", "beetles-card", "Young beetles on cardboard", "Chestnut coats",
            "We lift beetles onto card so you can count them. Pale cream first, then tan, then the dark armour of an adult."),
        Photo("mealworms", "beetle-among", "A tan beetle among larvae and oats", "Back in the tray",
            "Adults live in the same oats as the young. They lay eggs in the bedding. The next larvae hatch where their parents are still walking."),
        Video("mealworms", "tray-hide", "Trays with cardboard hide and carrots", "Hide and moisture",
            "A scrap of card gives beetles a roof. Carrot keeps them hydrated. Simple kit — buckets, oats, and kitchen leftovers."),
        Photo("mealworms", "beetles-dark", "Dark adult beetles in a sieve", "Grown up",
            "Mature darkling beetles are almost black. We sieve them when we want to show the adult stage, or to move a breeding group."),
        Photo("mealworms", "beetle-dark-close", "Close-up of a dark mealworm beetle", "The darkling beetle",
            "This is Tenebrio molitor, the beetle at the end of the line. From here the story starts again — eggs in the oats, then larvae."),
        Photo("mealworms", "frass", "Sifted mealworm frass in a blue bowl", "Frass — the byproduct",
            "Frass is the fine powder left after the worms have worked the oats. We sift it out. On the garden it is a gentle fertiliser. A little also goes back to the hens.")
    ];

    private static TourAlbumItem Photo(string folder, string name, string alt, string title, string caption) =>
        new($"/images/tours/{folder}/{name}.jpg", alt,
            FullSrc: $"/images/tours/{folder}/{name}-full.jpg",
            Title: title,
            Caption: caption);

    private static TourAlbumItem Video(string folder, string name, string alt, string title, string caption) =>
        new($"/images/tours/{folder}/{name}.jpg", alt,
            IsVideo: true,
            VideoSrc: $"/images/tours/{folder}/{name}.mp4",
            Title: title,
            Caption: caption);
}
