using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Toptanci.Application.Features.Auth;
using Toptanci.Application.Features.Catalog.Brands;
using Toptanci.Application.Features.Catalog.Categories;
using Toptanci.Application.Features.Catalog.Products;
using Toptanci.Application.Features.Catalog.Variants;
using Toptanci.Application.Features.Stock;
using Toptanci.Application.Features.Stock.ProductEntry;
using Toptanci.Application.Features.Stock.Queries;
using Toptanci.Application.Features.Stock.Warehouses;

namespace Toptanci.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);

        // Auth
        services.AddScoped<IAuthService, AuthService>();

        // Katalog
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IVariantService, VariantService>();

        // Stok
        services.AddScoped<IStockLedger, StockLedger>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IStockQueryService, StockQueryService>();
        services.AddScoped<IProductEntryService, ProductEntryService>();

        return services;
    }
}
