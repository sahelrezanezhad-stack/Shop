namespace Shop.Application.ShoppingCarts;
public sealed record CartLineDto(int LineId, int ProductId, string Slug, string Name,
    string Size, int Quantity, int StockQuantity, decimal UnitPriceToman,
    decimal LineTotalToman, string FrontImage);
public sealed record CartDto(IReadOnlyList<CartLineDto> Lines, int ItemCount, decimal TotalToman)
{
    public static CartDto Empty { get; } = new(Array.Empty<CartLineDto>(), 0, 0);
}
public sealed record CartOutcome(bool Success, string? Message, CartDto Cart)
{
    public static CartOutcome Error(string message, CartDto cart) => new(false, message, cart);
    public static CartOutcome Ok(CartDto cart) => new(true, null, cart);
}
