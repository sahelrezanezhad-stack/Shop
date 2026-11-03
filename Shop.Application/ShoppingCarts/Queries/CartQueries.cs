using Shop.Domain.Entities;
using Shop.Domain.Repositories;
namespace Shop.Application.ShoppingCarts.Queries;
public interface ICartQueries
{
    Task<CartDto> GetAsync(Guid cartId, CancellationToken ct = default);
}
public sealed class CartQueries(ICartRepository repository) : ICartQueries
{
    private readonly ICartRepository _repo = repository;
    internal static CartDto MakeDto(ShoppingCart? cart)
    {
        if (cart is null) return CartDto.Empty;
        var lines = cart.Lines.Where(l => l.Product is not null)
            .Select(l =>
            {
                var item = SallyCatalog.Find(l.Product.Name);
                return item is null ? null : new CartLineDto(
                    l.Id, l.ProductId, item.Slug, l.Product.Name, l.Size,
                    l.Quantity, l.Product.SizeStocks.FirstOrDefault(s => s.Size == l.Size)?.Quantity ?? 0, l.Product.Price,
                    l.Quantity * l.Product.Price, $"/images/collection/{item.FrontImage}");
            })
            .Where(l => l is not null)
            .Cast<CartLineDto>().OrderBy(l => l.LineId).ToList();
        return new CartDto(lines, lines.Sum(l => l.Quantity), lines.Sum(l => l.LineTotalToman));
    }
    public async Task<CartDto> GetAsync(Guid cartId, CancellationToken ct = default)
        => MakeDto(await _repo.GetAsync(cartId, ct));

}
