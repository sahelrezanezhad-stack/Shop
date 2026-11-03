using Shop.Domain.Entities;
using Shop.Domain.Repositories;
namespace Shop.Application.Orders.Queries;
public interface IOrderQueries
{
    Task<ManagedOrderPage> ListAsync(string? status, int page, CancellationToken ct);
    Task<ManagedOrder?> GetAsync(int id, CancellationToken ct);
}
public sealed class OrderQueries(IOrderRepository repository) : IOrderQueries
{
    private static ManagedOrder Map(Order order) => new(order.Id, order.OrderDate, order.Status, order.TotalAmount,
        (order.Customer.FirstName + " " + order.Customer.LastName).Trim(), order.Customer.PhoneNumber ?? "",
        order.Customer.Email, order.ShippingCity, order.ShippingAddress, order.ShippingPostalCode,
        order.Status == "PendingPayment" && order.SourceCartId.HasValue,
        order.Items.Select(x => new ManagedOrderLine(x.Product.Name, x.Size, x.Quantity, x.UnitPrice)).ToList());

    public async Task<ManagedOrderPage> ListAsync(string? status, int page, CancellationToken ct)
    {
        var result = await repository.ListAsync(status, page, ct);
        return new(result.Orders.Select(Map).ToList(), result.Page, result.TotalPages);
    }
    public async Task<ManagedOrder?> GetAsync(int id, CancellationToken ct)
    {
        var order = await repository.GetAsync(id, ct);
        return order is null ? null : Map(order);
    }
}
