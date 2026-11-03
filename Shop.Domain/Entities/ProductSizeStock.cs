namespace Shop.Domain.Entities;

/// <summary>Physical sellable inventory for one garment/size combination.</summary>
public sealed class ProductSizeStock
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Product Product { get; set; } = null!;
}
