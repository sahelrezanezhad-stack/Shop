using Shop.Domain.Entities;
using Shop.Domain.Repositories;
using Shop.Application.ShoppingCarts;
namespace Shop.Application.Inventory.Commands;
public interface IInventoryCommands
{
    Task<InventoryOutcome> SetAsync(int productId, int expectedTotal, IReadOnlyList<InventoryChange> changes, CancellationToken ct);
}
public sealed class InventoryCommands(IInventoryRepository repository) : IInventoryCommands
{
    private static CartMutation<InventoryOutcome> Reject(string message) => new(new(false, message), false);
    public Task<InventoryOutcome> SetAsync(int productId, int expectedTotal, IReadOnlyList<InventoryChange> changes, CancellationToken ct)
    {
        if (changes.Count is < 1 or > 5 || changes.Any(x => x.Quantity is < 0 or > 100000 || x.ExpectedQuantity < 0)
            || changes.Select(x => x.Size).Distinct(StringComparer.Ordinal).Count() != changes.Count)
            return Task.FromResult(new InventoryOutcome(false, "Enter valid size quantities between 0 and 100,000."));
        return repository.UpdateLockedAsync(productId, product =>
        {
            if (product is null) return Reject("The product was not found.");
            var catalog = SallyCatalog.Find(product.Name);
            if (catalog is null || !catalog.Sizes.OrderBy(x => x).SequenceEqual(changes.Select(x => x.Size).OrderBy(x => x)))
                return Reject("The supplied sizes do not match the approved product sizes.");
            if (product.SizeStocks.Any(x => !catalog.Sizes.Contains(x.Size)))
                return Reject("Unexpected existing sizes require review before inventory can be changed.");
            if (product.StockQuantity != expectedTotal || changes.Any(x =>
                    (product.SizeStocks.SingleOrDefault(s => s.Size == x.Size)?.Quantity ?? 0) != x.ExpectedQuantity))
                return Reject("Inventory changed while this page was open. Reload it before making changes.");
            foreach (var change in changes)
            {
                var stock = product.SizeStocks.SingleOrDefault(x => x.Size == change.Size);
                if (stock is null) product.SizeStocks.Add(new ProductSizeStock { Size = change.Size, Quantity = change.Quantity });
                else stock.Quantity = change.Quantity;
            }
            product.StockQuantity = changes.Sum(x => x.Quantity);
            return new CartMutation<InventoryOutcome>(new(true, "Size quantities and total inventory were updated together."), true);
        }, ct);
    }
}
