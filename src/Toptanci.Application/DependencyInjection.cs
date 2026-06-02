using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Toptanci.Application.Features.Auth;

namespace Toptanci.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
