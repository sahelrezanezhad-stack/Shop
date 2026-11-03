using Microsoft.EntityFrameworkCore;
using Shop.Domain.Entities;
using Shop.Domain.Repositories;
using Shop.Infrastructure.Persistence;
namespace Shop.Infrastructure.Repositories;
public sealed class OrderRepository(ShopDbContext db) : IOrderRepository
{
    public async Task<OrderPage> ListAsync(string? status, int page, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        var pages = Math.Max(1, (int)Math.Ceiling(await query.CountAsync(ct) / 30d));
        page = Math.Clamp(page, 1, pages);

        var orders = await query.Include(x => x.Customer).Include(x => x.Items).ThenInclude(x => x.Product)
            .OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.Id).Skip((page - 1) * 30).Take(30).ToListAsync(ct);
        return new OrderPage(orders, page, pages);
    }

    public async Task<Order?> GetAsync(int id, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(x => x.Customer)
            .Include(x => x.Items).ThenInclude(x => x.Product).SingleOrDefaultAsync(x => x.Id == id, ct);
        return order;
    }


    public async Task<T> WithLockAsync<T>(int id, Func<Order?, Task<CartMutation<T>>> action, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var found = await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE [Orders] SET [Status] = [Status] WHERE [Id] = {id}", ct);
        var order = found == 1 ? await db.Orders.Include(x => x.Items).Include(x => x.Customer).SingleAsync(x => x.Id == id, ct) : null;
        var result = await action(order);
        if (result.Commit)
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        return result.Value;
    }
    public async Task<bool> RestoreStockAsync(IReadOnlyList<OrderItem> items, CancellationToken ct)
    {
        foreach (var group in items.OrderBy(x => x.ProductId).GroupBy(x => x.ProductId))
        {
            var count = group.Sum(x => (long)x.Quantity);
            if (count > int.MaxValue) return false;
            var amount = (int)count;
            // The product-before-size lock order is shared with checkout and inventory editing.
            var productRows = await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [Products] SET [StockQuantity] = [StockQuantity] + {amount} WHERE [Id] = {group.Key} AND [StockQuantity] <= {int.MaxValue - amount}", ct);
            if (productRows != 1) return false;
            foreach (var line in group.OrderBy(x => x.Size))
            {
                var sizeRows = await db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE [ProductSizeStocks] SET [Quantity] = [Quantity] + {line.Quantity} WHERE [ProductId] = {line.ProductId} AND [Size] = {line.Size} AND [Quantity] <= {int.MaxValue - line.Quantity}", ct);
                if (sizeRows != 1) return false;
            }
        }
        return true;
    }
    public void QueueCancellation(Order order)
    {
        if (order.AccountId.HasValue || !string.IsNullOrWhiteSpace(order.Customer.Email))
            db.SallyEmailOutbox.Add(new SallyEmailOutbox { AccountId = order.AccountId ?? Guid.Empty,
                OrderId = order.Id, Kind = "OrderCancelled" });
    }
}
