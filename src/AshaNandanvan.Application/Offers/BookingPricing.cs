using AshaNandanvan.Domain.Entities;
using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Application.Offers;

public static class BookingPricing
{
    public static int NightCount(DateTimeOffset start, DateTimeOffset end)
    {
        var nights = (int)Math.Ceiling((end - start).TotalDays);
        return Math.Max(1, nights);
    }

    public static int NightCount(ProductSlot slot) => NightCount(slot.StartsAt, slot.EndsAt);

    public static decimal LineTotal(Product product, ProductSlot? slot, int quantity, DateTimeOffset? stayStart = null, DateTimeOffset? stayEnd = null, bool trialStay = false)
    {
        if (trialStay)
        {
            return 0;
        }

        if (product.Category == ProductCategory.DogSitting && stayStart is not null && stayEnd is not null)
        {
            return product.Price * NightCount(stayStart.Value, stayEnd.Value) * quantity;
        }

        if (product.Category == ProductCategory.DogSitting && slot is not null)
        {
            return product.Price * NightCount(slot) * quantity;
        }

        return product.Price * quantity;
    }

    public static decimal UnitPrice(Product product, ProductSlot? slot, DateTimeOffset? stayStart = null, DateTimeOffset? stayEnd = null, bool trialStay = false)
    {
        if (trialStay)
        {
            return 0;
        }

        if (product.Category == ProductCategory.DogSitting && stayStart is not null && stayEnd is not null)
        {
            return product.Price * NightCount(stayStart.Value, stayEnd.Value);
        }

        return product.Category == ProductCategory.DogSitting && slot is not null
            ? product.Price * NightCount(slot)
            : product.Price;
    }

    public static string StayLabel(DateTimeOffset dropOff, DateTimeOffset pickUp, string? petName = null, string? petBreed = null, bool trialStay = false)
    {
        var start = dropOff.ToOffset(SydneyOffset(dropOff));
        var end = pickUp.ToOffset(SydneyOffset(pickUp));
        var nights = NightCount(dropOff, pickUp);
        var dates = $"Drop-off {start:ddd d MMM, h:mm tt} · Pick-up {end:ddd d MMM, h:mm tt} · {nights} night{(nights == 1 ? "" : "s")}";
        if (trialStay)
        {
            dates = $"Free trial night · {dates}";
        }

        var who = string.Join(" · ", new[] { petName?.Trim(), petBreed?.Trim() }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(who) ? dates : $"{who} · {dates}";
    }

    public static string SlotLabel(ProductSlot slot, ProductCategory category)
    {
        if (!string.IsNullOrWhiteSpace(slot.Label))
        {
            return slot.Label;
        }

        if (category == ProductCategory.DogSitting)
        {
            return StayLabel(slot.StartsAt, slot.EndsAt);
        }

        var start = slot.StartsAt.ToOffset(SydneyOffset(slot.StartsAt));
        var end = slot.EndsAt.ToOffset(SydneyOffset(slot.EndsAt));
        return $"{start:ddd d MMM, h:mm tt} – {end:h:mm tt}";
    }

    public static TimeSpan SydneyOffset(DateTimeOffset at)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "AUS Eastern Standard Time" : "Australia/Sydney");
        return tz.GetUtcOffset(at.UtcDateTime);
    }
}
