
using System.ComponentModel.DataAnnotations;

namespace Shop.Application.Products.DTOs
{
    public class CreateProductDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        [Range(0.01, 999999999)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }
    }
}
