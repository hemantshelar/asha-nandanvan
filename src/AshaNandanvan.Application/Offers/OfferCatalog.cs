using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Application.Offers;

public sealed record OfferDefinition(
    string Slug,
    string Title,
    string Eyebrow,
    string Summary,
    string Description,
    string ImagePath,
    IReadOnlyList<ProductCategory> Categories,
    bool RequiresBooking)
{
    public bool Contains(ProductCategory category) => Categories.Contains(category);
}

public static class OfferCatalog
{
    public static readonly OfferDefinition FreshProduce = new(
        "fresh-produce",
        "Fresh garden produce",
        "Harvest",
        "Eggs and vegetables picked from the same Sydney beds.",
        "What is ready that week leaves the gate as eggs, greens, tomatoes, and whatever the beds are happiest growing. Admin manage this list — if it is not in the shop, it is still in the soil.",
        "/images/fresh-produce.jpg",
        [ProductCategory.Eggs, ProductCategory.Veggies],
        false);

    public static readonly OfferDefinition DogSitting = new(
        "dog-sitting",
        "Dog sitting",
        "Care",
        "Your dog stays with us in the backyard while you are away.",
        "Choose drop-off and pick-up. We can host up to five dogs at once — if those dates are already confirmed full, we will tell you before you pay.",
        "/images/asha.jpg",
        [ProductCategory.DogSitting],
        true);

    public static readonly OfferDefinition CompostTours = new(
        "composting-tours",
        "Composting education tours",
        "Learn",
        "Walk the loop: scraps, worms, castings, and the beds they feed.",
        "Walk the loop we run every week — worm bins, chicken coop, then the mealworm trays — while they are working. Ninety minutes, a small group, shoes you do not mind getting dusty.",
        "/images/compost-tour.jpg",
        [ProductCategory.CompostTour],
        true);

    public static readonly OfferDefinition WormsAndGold = new(
        "worms-and-black-gold",
        "Worms & black gold",
        "Take home",
        "Mealworms, frass, and bags of black gold — made in this backyard.",
        "Live mealworms for the hens, the fine frass they leave behind, and black gold from the worm farm. Same loop as the garden — you continue it at home.",
        "/images/worms-and-gold.jpg",
        [ProductCategory.WormsAndCompost],
        false);

    public static IReadOnlyList<OfferDefinition> All { get; } =
        [FreshProduce, DogSitting, CompostTours, WormsAndGold];

    public static OfferDefinition? Find(string slug) =>
        All.FirstOrDefault(o => o.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public static OfferDefinition? ForCategory(ProductCategory category) =>
        All.FirstOrDefault(o => o.Contains(category));
}
