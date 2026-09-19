namespace AshaNandanvan.Application.Cart;

public interface ICartService
{
    event Action? Changed;
    Task<CartSnapshot> GetAsync(CancellationToken cancellationToken = default);
    Task AddAsync(int productId, int quantity = 1, CancellationToken cancellationToken = default);
    Task UpdateQuantityAsync(int productId, int quantity, CancellationToken cancellationToken = default);
    Task RemoveAsync(int productId, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
    Task MergeGuestCartAsync(CancellationToken cancellationToken = default);
}
