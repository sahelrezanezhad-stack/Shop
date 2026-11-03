namespace Shop.Application.ShoppingCarts;
public interface ICartService
{
    Task<CartDto> GetAsync(Guid cartId, CancellationToken ct = default);
    Task<CartOutcome> AddAsync(Guid cartId, int productId, string? size, int quantity, CancellationToken ct = default);
    Task<CartOutcome> UpdateAsync(Guid cartId, int lineId, int quantity, CancellationToken ct = default);
    Task<CartOutcome> RemoveAsync(Guid cartId, int lineId, CancellationToken ct = default);
}
