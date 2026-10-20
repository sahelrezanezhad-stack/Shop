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

        public async Task UpdateAsync(UpdateProductDto dto)
        {
            var product = await _repository.GetByIdForUpdateAsync(dto.Id);

            if (product == null)
                throw new KeyNotFoundException("Product not found.");

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;

            await _repository.UpdateAsync(product);
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _repository.GetByIdForUpdateAsync(id);

            if (product == null)
                throw new KeyNotFoundException("Product not found.");

            await _repository.DeleteAsync(product);
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
}
