using Shop.Domain.Orders;
namespace Shop.Application.Orders;

public sealed record CheckoutInput(string? FullName, string? Phone, string? City,
    string? Address, string? PostalCode, string? Email);
public interface ICheckoutService
{
    Task<OrderOutcome> PlaceAsync(Guid cartId, CheckoutInput input, Guid? accountId = null, CancellationToken ct = default);
}
