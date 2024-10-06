using Microsoft.Extensions.DependencyInjection;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Infrastructure.Services;
using Pango.Persistence;
using Pango.Persistence.File;

namespace Pango.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IContentEncoder, ContentEncoder>();
        services.AddFileStorage();

        return services;
    }
}
