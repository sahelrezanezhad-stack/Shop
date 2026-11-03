using Shop.Domain.Entities;
namespace Shop.Domain.Repositories;
public sealed record OrderPage(IReadOnlyList<Order> Orders, int Page, int TotalPages);
public interface IOrderRepository
{
    Task<OrderPage> ListAsync(string? status, int page, CancellationToken ct);
    Task<Order?> GetAsync(int id, CancellationToken ct);
    Task<T> WithLockAsync<T>(int id, Func<Order?, Task<CartMutation<T>>> action, CancellationToken ct);
    Task<bool> RestoreStockAsync(IReadOnlyList<OrderItem> items, CancellationToken ct);
    void QueueCancellation(Order order);
}
