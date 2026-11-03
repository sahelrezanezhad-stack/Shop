using Shop.Application.ShoppingCarts.Commands;
using Shop.Application.ShoppingCarts.Queries;
namespace Shop.Application.ShoppingCarts;
public sealed class CartService(ICartQueries queries, ICartCommands commands) : ICartService
{
    public Task<CartDto> GetAsync(Guid cartId, CancellationToken ct = default) => queries.GetAsync(cartId, ct);
    public Task<CartOutcome> AddAsync(Guid cartId, int productId, string? size, int quantity, CancellationToken ct = default)
        => commands.AddAsync(cartId, productId, size, quantity, ct);
    public Task<CartOutcome> UpdateAsync(Guid cartId, int lineId, int quantity, CancellationToken ct = default)
        => commands.UpdateAsync(cartId, lineId, quantity, ct);
    public Task<CartOutcome> RemoveAsync(Guid cartId, int lineId, CancellationToken ct = default)
        => commands.RemoveAsync(cartId, lineId, ct);
}
