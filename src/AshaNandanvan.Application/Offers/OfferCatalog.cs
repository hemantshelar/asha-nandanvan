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
        "/images/greens.svg",
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
        "A small-group visit through the composting system we actually run. Choose a session, pay for your guests, and we will see you at the gate.",
        "/images/compost.svg",
        [ProductCategory.CompostTour],
        true);

    public static readonly OfferDefinition WormsAndGold = new(
        "worms-and-black-gold",
        "Worms & black gold",
        "Soil",
        "Take a starter colony or a bag of finished worm castings home.",
        "Live compost worms and the black gold they make. Same loop as the garden — you just continue it in your own backyard.",
        "/images/worms.svg",
        [ProductCategory.WormsAndCompost],
        false);

    public static IReadOnlyList<OfferDefinition> All { get; } =
        [FreshProduce, DogSitting, CompostTours, WormsAndGold];

    public static OfferDefinition? Find(string slug) =>
        All.FirstOrDefault(o => o.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public static OfferDefinition? ForCategory(ProductCategory category) =>
        All.FirstOrDefault(o => o.Contains(category));
}
