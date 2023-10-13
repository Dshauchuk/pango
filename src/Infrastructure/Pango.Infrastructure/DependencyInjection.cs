using Microsoft.Extensions.DependencyInjection;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Infrastructure.Services;
using Pango.Persistence;

namespace Pango.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordRepository, PasswordRepository>();
        services.AddScoped<IContentEncoder, ContentEncoder>();

        return services;
    }
}
