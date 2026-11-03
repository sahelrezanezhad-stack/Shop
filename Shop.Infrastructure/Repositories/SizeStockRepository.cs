using Shop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shop.Infrastructure.Persistence;
namespace Shop.Infrastructure.Repositories;
public sealed class SizeStockRepository : ISizeStockRepository
{
    private readonly ShopDbContext _db;
    public SizeStockRepository(ShopDbContext db) => _db = db;
    public async Task<IReadOnlyList<SizeAvailability>> ListAsync(CancellationToken ct = default)
        => await _db.ProductSizeStocks.AsNoTracking()
            .Select(s => new SizeAvailability(s.ProductId, s.Size, s.Quantity))
            .ToListAsync(ct);
    public Task<int> AvailableAsync(int productId, string size, CancellationToken ct = default)
        => _db.ProductSizeStocks.AsNoTracking()
            .Where(s => s.ProductId == productId && s.Size == size)
            .Select(s => s.Quantity).FirstOrDefaultAsync(ct);
}
