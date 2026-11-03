using Shop.Domain.Repositories;
using Shop.Application.Products;
using Shop.Domain.Entities;
using Shop.Application.ShoppingCarts.Queries;
namespace Shop.Application.ShoppingCarts.Commands;

/// <summary>Persisted guest carts; product price and inventory always come from SQL Server.</summary>
public interface ICartCommands
{
    Task<CartOutcome> AddAsync(Guid cartId, int productId, string? size, int quantity, CancellationToken ct = default);
    Task<CartOutcome> UpdateAsync(Guid cartId, int lineId, int quantity, CancellationToken ct = default);
    Task<CartOutcome> RemoveAsync(Guid cartId, int lineId, CancellationToken ct = default);
}
public sealed class CartCommands : ICartCommands
{
    private readonly ICartRepository _repo;
    private readonly IProductService _products;
    private readonly ISizeStockRepository _stock;
    private readonly ICartQueries _queries;
    public CartCommands(ICartRepository repo, IProductService products, ISizeStockRepository stock, ICartQueries queries)
        => (_repo, _products, _stock, _queries) = (repo, products, stock, queries);

    private Task<CartOutcome> WithLockAsync(Guid cartId, bool create, Func<Task<CartOutcome>> action, CancellationToken ct)
        => _repo.WithLockAsync(cartId, create, async () =>
        {
            if (await _repo.HasCheckoutAsync(cartId, ct))
                return new CartMutation<CartOutcome>(CartOutcome.Error(
                    "This shopping bag has already been checked out. Refresh the page to continue shopping.", CartDto.Empty), false);
            var outcome = await action();
            return new CartMutation<CartOutcome>(outcome, outcome.Success);
        }, ct);

    private static CartDto MakeDto(ShoppingCart? cart) => CartQueries.MakeDto(cart);
    private Task<CartDto> GetAsync(Guid cartId, CancellationToken ct) => _queries.GetAsync(cartId, ct);

    public Task<CartOutcome> AddAsync(Guid cartId, int productId, string? size, int quantity, CancellationToken ct = default)
        => WithLockAsync(cartId, true, () => AddCoreAsync(cartId, productId, size, quantity, ct), ct);

    private async Task<CartOutcome> AddCoreAsync(Guid cartId, int productId, string? size, int quantity, CancellationToken ct)
    {
        if (quantity is < 1 or > 10 || string.IsNullOrWhiteSpace(size))
            return CartOutcome.Error("The quantity or size is invalid.", await GetAsync(cartId, ct));
        size = size.Trim().ToUpperInvariant();
        var product = await _products.GetByIdAsync(productId);
        var catalog = product is null ? null : SallyCatalog.Find(product.Name);
        if (product is null || catalog is null)
            return CartOutcome.Error("The selected product was not found.", await GetAsync(cartId, ct));
        if (!catalog.Sizes.Contains(size, StringComparer.Ordinal))
            return CartOutcome.Error("This size is not available for the selected product.", await GetAsync(cartId, ct));
        var cart = await _repo.GetAsync(cartId, ct);
        var line = cart?.Lines.FirstOrDefault(l => l.ProductId == productId && l.Size == size);
        var nextQuantity = (line?.Quantity ?? 0) + quantity;
        if (nextQuantity > 10 || nextQuantity > await _stock.AvailableAsync(productId, size, ct))
            return CartOutcome.Error("There is not enough stock available for this size.", MakeDto(cart));
        if (cart is null)
        {
            cart = new ShoppingCart { Id = cartId };
            await _repo.AddAsync(cart, ct);
        }
        if (line is null)
            cart.Lines.Add(new ShoppingCartLine { ProductId = productId, Size = size, Quantity = quantity });
        else line.Quantity = nextQuantity;
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await _repo.SaveAsync(ct);
        return CartOutcome.Ok(MakeDto(await _repo.GetAsync(cartId, ct)));
    }

    public Task<CartOutcome> UpdateAsync(Guid cartId, int lineId, int quantity, CancellationToken ct = default)
        => WithLockAsync(cartId, false, () => UpdateCoreAsync(cartId, lineId, quantity, ct), ct);

    private async Task<CartOutcome> UpdateCoreAsync(Guid cartId, int lineId, int quantity, CancellationToken ct)
    {
        var cart = await _repo.GetAsync(cartId, ct);
        if (cart is null) return CartOutcome.Error("The shopping bag could not be found.", CartDto.Empty);
        var line = cart.Lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null) return CartOutcome.Error("The selected bag item could not be found.", MakeDto(cart));
        if (quantity is < 0 or > 10)
            return CartOutcome.Error("Quantity must be between zero and ten.", MakeDto(cart));
        if (quantity > 0 && quantity > await _stock.AvailableAsync(line.ProductId, line.Size, ct))
            return CartOutcome.Error("There is not enough stock available.", MakeDto(cart));
        if (quantity == 0) cart.Lines.Remove(line);
        else line.Quantity = quantity;
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await _repo.SaveAsync(ct);
        return CartOutcome.Ok(MakeDto(await _repo.GetAsync(cartId, ct)));
    }

    public async Task<CartOutcome> RemoveAsync(Guid cartId, int lineId, CancellationToken ct = default)
        => await UpdateAsync(cartId, lineId, 0, ct);
}
