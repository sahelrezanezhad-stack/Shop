using Shop.Domain.Entities;
namespace Shop.Domain.Repositories;
public interface IInventoryRepository
{
    Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct);
    Task<T> UpdateLockedAsync<T>(int productId, Func<Product?, CartMutation<T>> change, CancellationToken ct);
}
