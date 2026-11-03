namespace Shop.Domain.Entities;
public sealed class ShoppingCartLine
{
    public int Id { get; set; }
    public Guid ShoppingCartId { get; set; }
    public int ProductId { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public ShoppingCart ShoppingCart { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
