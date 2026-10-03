namespace AshaNandanvan.Application.Cart;

public sealed record CartLine(
    string LineKey,
    int ProductId,
    int? SlotId,
    string Name,
    string Slug,
    string Unit,
    decimal UnitPrice,
    int Quantity,
    int Stock,
    string? ImagePath,
    string? SlotLabel,
    bool IsStay = false,
    DateTimeOffset? StayStartsAt = null)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record CartSnapshot(IReadOnlyList<CartLine> Items)
{
    public int ItemCount => Items.Sum(i => i.Quantity);
    public decimal Total => Items.Sum(i => i.LineTotal);
    public bool IsEmpty => Items.Count == 0;
}
