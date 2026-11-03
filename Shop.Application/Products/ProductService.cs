using Shop.Application.Products.Commands;
using Shop.Application.Products.Queries;
using Shop.Application.Products.DTOs;
namespace Shop.Application.Products;

// Compatibility facade; command and query implementations are registered independently.
public sealed class ProductService(IProductQueries queries, IProductCommands commands) : IProductService
{
    public Task<List<ProductDto>> GetAllAsync() => queries.GetAllAsync();
    public Task<ProductDto?> GetByIdAsync(int id) => queries.GetByIdAsync(id);
    public Task<List<ProductDto>> SearchByNameAsync(string name) => queries.SearchByNameAsync(name);
    public Task<ProductDto> AddAsync(CreateProductDto dto) => commands.AddAsync(dto);
    public Task UpdateAsync(UpdateProductDto dto) => commands.UpdateAsync(dto);
    public Task DeleteAsync(int id) => commands.DeleteAsync(id);
}
