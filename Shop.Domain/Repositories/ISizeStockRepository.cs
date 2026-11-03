namespace Shop.Domain.Repositories;
public sealed record SizeAvailability(int ProductId, string Size, int Quantity);
public interface ISizeStockRepository
{
    Task<int> AvailableAsync(int productId, string size, CancellationToken ct = default);
    Task<IReadOnlyList<SizeAvailability>> ListAsync(CancellationToken ct = default);
}
