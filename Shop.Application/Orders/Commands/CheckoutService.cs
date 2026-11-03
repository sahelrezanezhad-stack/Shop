using Shop.Domain.Repositories;
using Shop.Domain.Orders;
using System.Text.RegularExpressions;
using Shop.Domain.Entities;
using Shop.Application.ShoppingCarts;
namespace Shop.Application.Orders.Commands;

public sealed class CheckoutService : ICheckoutService
{
    private readonly ICheckoutRepository _orders;
    public CheckoutService(ICheckoutRepository orders) => _orders = orders;

    public Task<OrderOutcome> PlaceAsync(Guid cartId, CheckoutInput input, Guid? accountId = null, CancellationToken ct = default)
    {
        if (cartId == Guid.Empty) return Task.FromResult(OrderOutcome.Fail("The shopping bag is invalid."));
        var name = input.FullName?.Trim() ?? "";
        var phone = (input.Phone ?? "").Replace(" ", "").Replace("-", "").Trim();
        var city = input.City?.Trim() ?? "";
        var address = input.Address?.Trim() ?? "";
        var postal = input.PostalCode?.Trim();
        var email = input.Email?.Trim() ?? "";
        if (name.Length is < 3 or > 100 || city.Length is < 2 or > 100 || address.Length is < 8 or > 300)
            return Task.FromResult(OrderOutcome.Fail("The name, city, or delivery address is invalid."));
        if (!Regex.IsMatch(phone, @"^\+?[0-9]{10,15}$"))
            return Task.FromResult(OrderOutcome.Fail("The phone number is invalid."));
        if (postal?.Length > 20 || email.Length > 180 || (email.Length > 0 && !Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$")))
            return Task.FromResult(OrderOutcome.Fail("The postal code or email address is invalid."));
        return _orders.PlaceAsync(cartId, new ValidatedCheckout(name, phone, city, address, postal, email), accountId, ValidateLines, ct);
    }

    private static string? ValidateLines(IReadOnlyList<ShoppingCartLine> lines)
    {
        if (lines.Count > 30) return "The shopping bag contains too many items.";
        foreach (var line in lines)
        {
            var catalog = SallyCatalog.Find(line.Product.Name);
            if (catalog is null || !catalog.Sizes.Contains(line.Size, StringComparer.Ordinal)
                || line.Quantity is < 1 or > 10)
                return "One of the products or sizes in your bag is invalid.";
        }
        return null;
    }
}
