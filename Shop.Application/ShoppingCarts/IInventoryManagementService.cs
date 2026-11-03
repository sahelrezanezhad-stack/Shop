namespace Shop.Application.ShoppingCarts;

public sealed record ManagedStockProduct(int Id, string Name, int Total, IReadOnlyList<ManagedSizeStock> Sizes);
public sealed record ManagedSizeStock(string Size, int Quantity);
public sealed record InventoryChange(string Size, int ExpectedQuantity, int Quantity);
public sealed record InventoryOutcome(bool Success, string Message);

public interface IInventoryManagementService
{
    Task<IReadOnlyList<ManagedStockProduct>> ListAsync(CancellationToken ct);
    Task<InventoryOutcome> SetAsync(int productId, int expectedTotal, IReadOnlyList<InventoryChange> changes, CancellationToken ct);
}
