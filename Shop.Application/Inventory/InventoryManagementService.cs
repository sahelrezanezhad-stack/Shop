using Shop.Application.ShoppingCarts;
using Shop.Application.Inventory.Commands;
using Shop.Application.Inventory.Queries;
namespace Shop.Application.Inventory;
public sealed class InventoryManagementService(IInventoryQueries queries, IInventoryCommands commands) : IInventoryManagementService
{
    public Task<IReadOnlyList<ManagedStockProduct>> ListAsync(CancellationToken ct) => queries.ListAsync(ct);
    public Task<InventoryOutcome> SetAsync(int productId, int expectedTotal, IReadOnlyList<InventoryChange> changes, CancellationToken ct)
        => commands.SetAsync(productId, expectedTotal, changes, ct);
}
