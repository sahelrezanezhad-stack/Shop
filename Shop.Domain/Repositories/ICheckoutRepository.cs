using Shop.Domain.Orders;
using Shop.Domain.Entities;
namespace Shop.Domain.Repositories;
public interface ICheckoutRepository
{
    Task<OrderOutcome> PlaceAsync(Guid cartId, ValidatedCheckout info, Guid? accountId, Func<IReadOnlyList<ShoppingCartLine>, string?> validateLines, CancellationToken ct = default);
}
