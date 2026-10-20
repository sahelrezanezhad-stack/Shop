using Shop.Application.Products.DTOs;
using Shop.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Application.Products
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllAsync();
        Task<List<Product>> GetAllEntitiesAsync();
        Task<ProductDto?> GetByIdAsync(int id);
        Task<Product?> GetEntityByIdAsync(int id);
        Task<ProductDto> AddAsync(CreateProductDto dto);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Product product);
    }
}
