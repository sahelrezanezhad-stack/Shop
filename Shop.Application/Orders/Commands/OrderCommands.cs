using Shop.Domain.Repositories;
namespace Shop.Application.Orders.Commands;
public interface IOrderCommands
{
    Task<ManagementOutcome> CancelUnpaidAsync(int id, CancellationToken ct);
}
public sealed class OrderCommands(IOrderRepository repository) : IOrderCommands
{
    private static CartMutation<ManagementOutcome> Result(bool success, string message, bool commit = false)
        => new(new(success, message), commit);
    public Task<ManagementOutcome> CancelUnpaidAsync(int id, CancellationToken ct)
        => repository.WithLockAsync(id, async order =>
        {
            if (order is null) return Result(false, "The order was not found.");
            if (order.Status == "Cancelled") return Result(true, "This order is already cancelled.");
            if (order.Status != "PendingPayment" || !order.SourceCartId.HasValue)
                return Result(false, "Only unpaid SALLY checkout orders can be cancelled here.");
            if (order.Items.Count == 0 || order.Items.Any(x => x.Quantity < 1 || string.IsNullOrWhiteSpace(x.Size)))
                return Result(false, "Inventory could not be verified. The order was left unchanged.");
            if (!await repository.RestoreStockAsync(order.Items.ToList(), ct))
                return Result(false, "Product or size inventory could not be restored. Nothing was changed.");
            order.Status = "Cancelled";
            repository.QueueCancellation(order);
            return Result(true, "The unpaid order was cancelled and its stock was restored.", true);
        }, ct);
}
