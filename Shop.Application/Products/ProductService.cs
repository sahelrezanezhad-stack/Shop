using Shop.Application.Products.DTOs;
using Shop.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Application.Products
{
    public class ProductService:IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }


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
        public async Task<List<Product>> GetAllEntitiesAsync()
        {
            return await _repository.GetAllAsync();
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
        public async Task<Product?> GetEntityByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<ProductDto> AddAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
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

        public async Task UpdateAsync(Product product)
        {
            await _repository.UpdateAsync(product);
        }

        public async Task DeleteAsync(Product product)
        {
            await _repository.DeleteAsync(product);
        }
    }
}
