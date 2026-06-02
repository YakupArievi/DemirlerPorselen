using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Toptanci.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext, repository ve diğer altyapı servisleri sonraki fazlarda buraya eklenecek.
        // (Faz 0.2: DbContext + audit interceptor, Faz 1+: repository'ler)

        return services;
    }
}
