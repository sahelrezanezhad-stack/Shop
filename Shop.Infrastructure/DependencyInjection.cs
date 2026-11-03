using Shop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shop.Application.Products;
using Shop.Infrastructure.Persistence;
using Shop.Infrastructure.Repositories;
using Shop.Application.ShoppingCarts;

using Shop.Application.Orders;


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<ISizeStockRepository, SizeStockRepository>();
            services.AddScoped<ICheckoutRepository, CheckoutRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IInventoryRepository, InventoryRepository>();

            return services;
        }
    }

}
