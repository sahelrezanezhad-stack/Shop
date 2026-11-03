using Shop.Domain.Repositories;
using Shop.Application.ShoppingCarts;
namespace Shop.Application.Inventory.Queries;
public interface IInventoryQueries
{
    Task<IReadOnlyList<ManagedStockProduct>> ListAsync(CancellationToken ct);
}
public sealed class InventoryQueries(IInventoryRepository repository) : IInventoryQueries
{
    public async Task<IReadOnlyList<ManagedStockProduct>> ListAsync(CancellationToken ct)
    {
        var products = await repository.ListAsync(ct);
        return products.Where(x => SallyCatalog.Find(x.Name) is not null).Select(x => new ManagedStockProduct(x.Id, x.Name,
            x.StockQuantity, SallyCatalog.Find(x.Name)!.Sizes.Select(s => new ManagedSizeStock(s,
                x.SizeStocks.SingleOrDefault(v => v.Size == s)?.Quantity ?? 0)).ToList())).ToList();
    }
}
