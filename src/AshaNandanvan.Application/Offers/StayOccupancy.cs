namespace AshaNandanvan.Application.Offers;

public static class StayOccupancy
{
    public static int MaxConcurrent(
        IEnumerable<(DateTimeOffset Start, DateTimeOffset End, int Quantity)> stays,
        DateTimeOffset from,
        DateTimeOffset to)
    {
        var events = new List<(DateTimeOffset At, int Delta)>();
        foreach (var stay in stays)
        {
            var start = stay.Start > from ? stay.Start : from;
            var end = stay.End < to ? stay.End : to;
            if (end <= start || stay.Quantity <= 0)
            {
                continue;
            }

            events.Add((start, stay.Quantity));
            events.Add((end, -stay.Quantity));
        }

        events.Sort((left, right) =>
        {
            var compare = left.At.CompareTo(right.At);
            return compare != 0 ? compare : left.Delta.CompareTo(right.Delta);
        });

        var current = 0;
        var max = 0;
        foreach (var item in events)
        {
            current += item.Delta;
            if (current > max)
            {
                max = current;
            }
        }

        return max;
    }
}
