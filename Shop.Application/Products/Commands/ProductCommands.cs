using Shop.Domain.Repositories;
using Shop.Domain.Entities;
using Shop.Application.Products.DTOs;
using Shop.Application.ShoppingCarts;
namespace Shop.Application.Products.Commands;
public interface IProductCommands
{
    Task<ProductDto> AddAsync(CreateProductDto dto);
    Task UpdateAsync(UpdateProductDto dto);
    Task DeleteAsync(int id);
}
public sealed class ProductCommands(IProductRepository repository) : IProductCommands
{
    private readonly IProductRepository _repository = repository;
        public async Task<ProductDto> AddAsync(CreateProductDto dto)
        {
            if(dto.StockQuantity!=0)throw new InvalidOperationException("New products start with zero inventory. Use size inventory for approved collection products.");
            if((await _repository.SearchByNameAsync(dto.Name.Trim())).Any(x=>string.Equals(x.Name,dto.Name.Trim(),StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A product with this name already exists.");
            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(product);

            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                CreatedAt = product.CreatedAt
            };
        }

        public async Task UpdateAsync(UpdateProductDto dto)
        {
            var product = await _repository.GetByIdForUpdateAsync(dto.Id);

            if (product == null)
                throw new KeyNotFoundException("Product not found.");

            if(dto.StockQuantity!=product.StockQuantity)
                throw new InvalidOperationException("Inventory changed or was edited directly. Reload this product and use size inventory to change quantities.");
            if(SallyCatalog.Find(product.Name) is not null && !string.Equals(product.Name,dto.Name.Trim(),StringComparison.Ordinal))
                throw new InvalidOperationException("The name of an approved collection product is fixed. Contact the collection editor to change it.");

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            // Stock is written only through the atomic size inventory workflow or checkout.

            await _repository.UpdateAsync(product);
        }

        public Task DeleteAsync(int id)
        {
            throw new InvalidOperationException("Product deletion is disabled to protect order history.");
        }

}
