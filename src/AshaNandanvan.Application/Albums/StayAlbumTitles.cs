using AshaNandanvan.Application.Offers;
using AshaNandanvan.Domain.Entities;

namespace AshaNandanvan.Application.Albums;

public static class StayAlbumTitles
{
    public static string FromStay(Order order)
    {
        var line = order.Items
            .Where(i => i.StayStartsAt is not null && i.StayEndsAt is not null)
            .OrderBy(i => i.StayStartsAt)
            .FirstOrDefault();
        if (line?.StayStartsAt is null || line.StayEndsAt is null)
        {
            return $"Stay-{order.OrderNumber}";
        }

        var pets = PetNames(order);
        var pet = pets.Count == 0 ? "Stay" : string.Join("-and-", pets.Select(Sanitize));
        var start = line.StayStartsAt.Value.ToOffset(BookingPricing.SydneyOffset(line.StayStartsAt.Value));
        var end = line.StayEndsAt.Value.ToOffset(BookingPricing.SydneyOffset(line.StayEndsAt.Value));
        return $"{pet}-{start:ddMMM}-{end:ddMMM}";
    }

    public static string StayLabel(Order order)
    {
        var line = order.Items
            .Where(i => i.StayStartsAt is not null && i.StayEndsAt is not null)
            .OrderBy(i => i.StayStartsAt)
            .FirstOrDefault();
        if (line?.StayStartsAt is null || line.StayEndsAt is null)
        {
            return order.OrderNumber;
        }

        return BookingPricing.StayLabel(
            line.StayStartsAt.Value,
            line.StayEndsAt.Value,
            PetName(order),
            line.PetBreed,
            line.IsTrialStay);
    }

    public static string PetName(Order order)
    {
        var names = PetNames(order);
        return names.Count == 0 ? "Your dog"
            : names.Count == 1 ? names[0]
            : string.Join(" and ", names);
    }

    private static IReadOnlyList<string> PetNames(Order order) =>
        order.Items
            .Select(i => i.PetName?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string Sanitize(string name)
    {
        var trimmed = name.Trim();
        var chars = trimmed.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or ' ').ToArray();
        var cleaned = new string(chars).Replace(' ', '-');
        return string.IsNullOrWhiteSpace(cleaned) ? "Stay" : cleaned;
    }
}
