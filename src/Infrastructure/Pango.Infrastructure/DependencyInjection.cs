using Microsoft.Extensions.DependencyInjection;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Infrastructure.Data;

namespace Pango.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
