namespace AshaNandanvan.Application.Cart;

public sealed record CartLine(
    int ProductId,
    int? SlotId,
    string Name,
    string Slug,
    string Unit,
    decimal UnitPrice,
    int Quantity,
    int Stock,
    string? ImagePath,
    string? SlotLabel)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record CartSnapshot(IReadOnlyList<CartLine> Items)
{
    public int ItemCount => Items.Sum(i => i.Quantity);
    public decimal Total => Items.Sum(i => i.LineTotal);
    public bool IsEmpty => Items.Count == 0;
}
