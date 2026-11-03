using Shop.Application.ShoppingCarts.Commands;
using Shop.Application.ShoppingCarts.Queries;
using Shop.Application.Inventory;
using Shop.Application.Inventory.Commands;
using Shop.Application.Inventory.Queries;
using Shop.Application.Orders.Commands;
using Shop.Application.Orders.Queries;
using Microsoft.Extensions.DependencyInjection;
using Shop.Application.Products;
using Shop.Application.Products.Commands;
using Shop.Application.Products.Queries;
using Shop.Application.ShoppingCarts;
using Shop.Application.Orders;

namespace Shop.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddScoped<IProductQueries, ProductQueries>();
            services.AddScoped<IProductCommands, ProductCommands>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICartQueries, CartQueries>();
            services.AddScoped<ICartCommands, CartCommands>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<ICheckoutService, CheckoutService>();
            services.AddScoped<IOrderQueries, OrderQueries>();
            services.AddScoped<IOrderCommands, OrderCommands>();
            services.AddScoped<IOrderManagementService, OrderManagementService>();
            services.AddScoped<IInventoryQueries, InventoryQueries>();
            services.AddScoped<IInventoryCommands, InventoryCommands>();
            services.AddScoped<IInventoryManagementService, InventoryManagementService>();

            return services;
        }
    }
}