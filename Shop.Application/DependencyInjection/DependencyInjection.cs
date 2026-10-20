using Microsoft.Extensions.DependencyInjection;
using Shop.Application.Products;

namespace Shop.Application.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(
            this IServiceCollection services)
        {
            services.AddScoped<IProductService, ProductService>();

            return services;
        }
    }
}