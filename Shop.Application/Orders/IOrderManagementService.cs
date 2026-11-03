namespace Shop.Application.Orders;

public sealed record ManagedOrderLine(string Name, string Size, int Quantity, decimal UnitPriceToman);
public sealed record ManagedOrder(int Id, DateTime CreatedUtc, string Status, decimal TotalToman,
    string CustomerName, string Phone, string Email, string City, string Address, string? PostalCode,
    bool CanCancel, IReadOnlyList<ManagedOrderLine> Lines);
public sealed record ManagedOrderPage(IReadOnlyList<ManagedOrder> Orders, int Page, int TotalPages);
public sealed record ManagementOutcome(bool Success, string Message);

public interface IOrderManagementService
{
    Task<ManagedOrderPage> ListAsync(string? status, int page, CancellationToken ct);
    Task<ManagedOrder?> GetAsync(int id, CancellationToken ct);
    Task<ManagementOutcome> CancelUnpaidAsync(int id, CancellationToken ct);
}
