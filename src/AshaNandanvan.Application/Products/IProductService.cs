using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductListItem>> GetActiveAsync(ProductCategory? category = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductListItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductListItem?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ProductListItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ProductEditModel model, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProductEditModel model, CancellationToken cancellationToken = default);
}
