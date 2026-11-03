namespace Shop.Domain.Entities;
public sealed class ShoppingCart
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ShoppingCartLine> Lines { get; set; } = new List<ShoppingCartLine>();
}
