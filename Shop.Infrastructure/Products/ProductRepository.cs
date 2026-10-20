using Microsoft.EntityFrameworkCore;
using Shop.Application.Products;
using Shop.Domain.Entities;
using Shop.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shop.Infrastructure.Products
{
    public class ProductRepository : IProductRepository
    {
        private readonly ShopDbContext _context;

        public ProductRepository(ShopDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        public async Task<Product?> GetByIdForUpdateAsync(int id)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Product product)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Product>> SearchByNameAsync(string name)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.Name.Contains(name))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

    }
}
