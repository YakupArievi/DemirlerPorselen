using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Toptanci.Application.Features.Auth;
using Toptanci.Application.Features.Catalog.Brands;
using Toptanci.Application.Features.Catalog.Categories;
using Toptanci.Application.Features.Catalog.Products;
using Toptanci.Application.Features.Catalog.Variants;
using Toptanci.Application.Features.Operations.Broken;
using Toptanci.Application.Features.Operations.Counts;
using Toptanci.Application.Features.Operations.Transfers;
using Toptanci.Application.Features.Reporting.Dashboard;
using Toptanci.Application.Features.Reporting.Reports;
using Toptanci.Application.Features.Sales;
using Toptanci.Application.Features.Sales.Accounts;
using Toptanci.Application.Features.Sales.Customers;
using Toptanci.Application.Features.Sales.Payments;
using Toptanci.Application.Features.Sales.Sales;
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
        services.AddScoped<IPriceService, PriceService>();

        // Stok
        services.AddScoped<IStockLedger, StockLedger>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IStockQueryService, StockQueryService>();
        services.AddScoped<IProductEntryService, ProductEntryService>();

        // Satış & Cari
        services.AddScoped<IAccountLedger, AccountLedger>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<IPaymentService, PaymentService>();

        // Operasyonlar (kırık, sayım, transfer)
        services.AddScoped<IBrokenProductService, BrokenProductService>();
        services.AddScoped<IStockCountService, StockCountService>();
        services.AddScoped<IWarehouseTransferService, WarehouseTransferService>();

        // Raporlama
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();

        // Tahmin (Faz 8)
        services.AddScoped<Features.Forecasting.IForecastService, Features.Forecasting.ForecastService>();

        // Mobil müşteri portalı (Faz 7)
        services.AddScoped<Features.Portal.IPortalAuthService, Features.Portal.PortalAuthService>();

        return services;
    }
}
