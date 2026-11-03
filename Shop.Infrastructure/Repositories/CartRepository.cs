using Shop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;
using Shop.Infrastructure.Persistence;
namespace Shop.Infrastructure.Repositories;
public sealed class CartRepository : ICartRepository
{
    private readonly ShopDbContext _db;
    public CartRepository(ShopDbContext db) => _db = db;
    public Task<ShoppingCart?> GetAsync(Guid cartId, CancellationToken ct = default)
        => _db.ShoppingCarts.Include(c => c.Lines).ThenInclude(l => l.Product).ThenInclude(p => p.SizeStocks)
            .FirstOrDefaultAsync(c => c.Id == cartId, ct);
    public async Task AddAsync(ShoppingCart cart, CancellationToken ct = default)
    {
        await _db.ShoppingCarts.AddAsync(cart, ct);
    }
    public async Task SaveAsync(CancellationToken ct = default) => await _db.SaveChangesAsync(ct);

    public Task<bool> HasCheckoutAsync(Guid cartId, CancellationToken ct = default)
        => _db.Orders.AnyAsync(x => x.SourceCartId == cartId, ct);

    public async Task<T> WithLockAsync<T>(Guid cartId, bool create, Func<Task<CartMutation<T>>> action, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        if (create)
        {
            var now = DateTime.UtcNow;
            // Range locks cover the not-yet-created cart, including two concurrent first additions.
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                IF NOT EXISTS (SELECT 1 FROM [ShoppingCarts] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = {cartId})
                INSERT INTO [ShoppingCarts] ([Id],[CreatedAtUtc],[UpdatedAtUtc]) VALUES ({cartId},{now},{now})", ct);
        }
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [ShoppingCarts] SET [UpdatedAtUtc] = [UpdatedAtUtc] WHERE [Id] = {cartId}", ct);
        var result = await action();
        if (result.Commit) await tx.CommitAsync(ct);
        return result.Value;
    }
}
