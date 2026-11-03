using Shop.Application.Orders.Commands;
using Shop.Application.Orders.Queries;
namespace Shop.Application.Orders;
public sealed class OrderManagementService(IOrderQueries queries, IOrderCommands commands) : IOrderManagementService
{
    public Task<ManagedOrderPage> ListAsync(string? status, int page, CancellationToken ct) => queries.ListAsync(status, page, ct);
    public Task<ManagedOrder?> GetAsync(int id, CancellationToken ct) => queries.GetAsync(id, ct);
    public Task<ManagementOutcome> CancelUnpaidAsync(int id, CancellationToken ct) => commands.CancelUnpaidAsync(id, ct);
}
