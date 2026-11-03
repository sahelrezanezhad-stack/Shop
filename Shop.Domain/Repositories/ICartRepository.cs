using Shop.Domain.Entities;
namespace Shop.Domain.Repositories;

public sealed record CartMutation<T>(T Value, bool Commit);
public interface ICartRepository
{
    Task<ShoppingCart?> GetAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task AddAsync(ShoppingCart cart, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task<bool> HasCheckoutAsync(Guid cartId, CancellationToken cancellationToken = default);
    Task<T> WithLockAsync<T>(Guid cartId, bool create, Func<Task<CartMutation<T>>> action,
        CancellationToken cancellationToken = default);
}
