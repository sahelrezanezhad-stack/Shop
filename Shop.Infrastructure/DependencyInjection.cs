
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shop.Infrastructure.Persistence;
using Shop.Infrastructure.Repositories;

namespace Shop.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ShopDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")));

            // Repository used by the newer Domain layer
            services.AddScoped<
                Shop.Domain.Repositories.IProductRepository,
                Shop.Infrastructure.Repositories.ProductRepository>();

            // Repository used by the older Application layer
            services.AddScoped<
                Shop.Application.Products.IProductRepository,
                Shop.Infrastructure.Products.ProductRepository>();

            services.AddScoped<
                Shop.Domain.Repositories.ICartRepository,
                CartRepository>();

            services.AddScoped<
                Shop.Domain.Repositories.ISizeStockRepository,
                SizeStockRepository>();

            services.AddScoped<
                Shop.Domain.Repositories.ICheckoutRepository,
                CheckoutRepository>();

            services.AddScoped<
                Shop.Domain.Repositories.IOrderRepository,
                OrderRepository>();

            services.AddScoped<
                Shop.Domain.Repositories.IInventoryRepository,
                InventoryRepository>();

            return services;
        }
    }
}
