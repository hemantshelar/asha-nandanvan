using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Web.Services;

public static class CatalogFormatting
{
    public static string CategoryName(ProductCategory category) => category switch
    {
        ProductCategory.Eggs => "Eggs",
        ProductCategory.Veggies => "Veggies",
        ProductCategory.WormsAndCompost => "Worms & compost",
        _ => category.ToString()
    };

    public static string Money(decimal amount) => amount.ToString("C");
}
