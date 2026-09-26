namespace AshaNandanvan.Application.Cart;

public interface ICartService
{
    event Action? Changed;
    Task<CartSnapshot> GetAsync(CancellationToken cancellationToken = default);
    Task AddAsync(
        int productId,
        int quantity = 1,
        int? slotId = null,
        DateTimeOffset? stayStart = null,
        DateTimeOffset? stayEnd = null,
        string? petName = null,
        string? petBreed = null,
        bool trialStay = false,
        DateTimeOffset? intendedStayStart = null,
        DateTimeOffset? intendedStayEnd = null,
        CancellationToken cancellationToken = default);
    Task UpdateQuantityAsync(int productId, int quantity, int? slotId = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(int productId, int? slotId = null, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
    Task MergeGuestCartAsync(CancellationToken cancellationToken = default);
}
