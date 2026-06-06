using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Infrastructure.Persistence;
using Toptanci.Infrastructure.Persistence.Interceptors;
using Toptanci.Infrastructure.Security;

namespace Toptanci.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            DbProviderConfigurator.Configure(
                options,
                configuration["Database:Provider"],
                configuration.GetConnectionString("DefaultConnection"));

            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());

            options.ConfigureWarnings(w =>
            {
                // Ledger ilişkileri (StockMovement→Variant, RefreshToken→User) zorunlu uçta soft-delete
                // query filter'ı ile etkileşir; bu kayıtları soft-delete etmediğimiz için uyarı zararsız.
                w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning);
                // Dinamik query-filter + sequence ile runtime'da yanlış pozitif olabiliyor; pending-change
                // kontrolünü 'dotnet ef migrations has-pending-model-changes' ile design-time'da yapıyoruz.
                w.Ignore(RelationalEventId.PendingModelChangesWarning);
            });
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextInitializer>();

        // Güvenlik
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<ISequenceGenerator, SequenceGenerator>();
        services.AddScoped<IBarcodeGenerator, BarcodeGenerator>();
        services.AddSingleton<IFileStorage, Storage.LocalFileStorage>();
        services.AddSingleton<IReportPdfService, Reporting.ReportPdfService>();

        return services;
    }
}
