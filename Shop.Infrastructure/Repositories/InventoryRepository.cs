using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;
using Shop.Infrastructure.Persistence;
namespace Shop.Infrastructure.Repositories;
public sealed class InventoryRepository(ShopDbContext db) : IInventoryRepository
{
    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct)
        => await db.Products.AsNoTracking().Include(x => x.SizeStocks).OrderBy(x => x.Name).ToListAsync(ct);
    public async Task<T> UpdateLockedAsync<T>(int productId, Func<Product?, CartMutation<T>> change, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var found = await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Products] SET [StockQuantity] = [StockQuantity] WHERE [Id] = {productId}", ct);
        var product = found == 1 ? await db.Products.Include(x => x.SizeStocks).SingleAsync(x => x.Id == productId, ct) : null;
        var result = change(product);
        if (result.Commit)
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        return result.Value;
    }
}
