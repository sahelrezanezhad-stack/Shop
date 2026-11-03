namespace Shop.Domain.Orders;

public sealed record ValidatedCheckout(string FullName, string Phone, string City,
    string Address, string? PostalCode, string Email);
public sealed record PlacedOrderLine(int ProductId, string Name, string Size,
    int Quantity, decimal UnitPriceToman, decimal LineTotalToman);
public sealed record PlacedOrder(int OrderId, string Status, decimal TotalToman,
    IReadOnlyList<PlacedOrderLine> Lines, bool NewlyCreated = false);
public sealed record OrderOutcome(bool Success, string? Message, string? ErrorCode, PlacedOrder? Order)
{
    public static OrderOutcome Fail(string message, string code="InvalidOrder") => new(false, message, code, null);
    public static OrderOutcome Ok(PlacedOrder order) => new(true, null, null, order);
}
