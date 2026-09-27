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

        var pet = string.IsNullOrWhiteSpace(line.PetName) ? "Stay" : Sanitize(line.PetName);
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
            line.PetName,
            line.PetBreed,
            line.IsTrialStay);
    }

    public static string PetName(Order order) =>
        order.Items
            .Select(i => i.PetName)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
            ?.Trim()
        ?? "Your dog";

    private static string Sanitize(string name)
    {
        var trimmed = name.Trim();
        var chars = trimmed.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or ' ').ToArray();
        var cleaned = new string(chars).Replace(' ', '-');
        return string.IsNullOrWhiteSpace(cleaned) ? "Stay" : cleaned;
    }
}
