namespace AshaNandanvan.Domain.Enums;

public enum ProductCategory
{
    Eggs = 1,
    Veggies = 2,
    WormsAndCompost = 3,
    DogSitting = 4,
    CompostTour = 5
}

public static class ProductCategoryExtensions
{
    public static bool RequiresBooking(this ProductCategory category) =>
        category is ProductCategory.DogSitting or ProductCategory.CompostTour;
}
