using Shop.Domain.Repositories;
using Shop.Domain.Entities;
using Shop.Application.Products.DTOs;
using Shop.Application.ShoppingCarts;
namespace Shop.Application.Products.Queries;
public interface IProductQueries
{
    Task<List<ProductDto>> GetAllAsync();
    Task<ProductDto?> GetByIdAsync(int id);
    Task<List<ProductDto>> SearchByNameAsync(string name);
}
public sealed class ProductQueries(IProductRepository repository) : IProductQueries
{
    private readonly IProductRepository _repository = repository;
        public async Task<List<ProductDto>> GetAllAsync()
        {
            var products = await _repository.GetAllAsync();

            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                CreatedAt = p.CreatedAt
            }).ToList();
        }


        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            if (id <= 0)
                return null;

            var product = await _repository.GetByIdAsync(id);

            if (product == null)
                return null;

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


        public async Task<List<ProductDto>> SearchByNameAsync(string name)
        {
            var products = await _repository.SearchByNameAsync(name);

            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                CreatedAt = p.CreatedAt
            }).ToList();
        }
}
