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
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());

            // Ledger ilişkileri (StockMovement→Variant, RefreshToken→User) zorunlu uçta soft-delete
            // query filter'ı ile etkileşir; bu kayıtları soft-delete etmediğimiz için uyarı zararsız.
            options.ConfigureWarnings(w =>
                w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ApplicationDbContextInitializer>();

        // Güvenlik
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IBarcodeGenerator, BarcodeGenerator>();

        return services;
    }
}
