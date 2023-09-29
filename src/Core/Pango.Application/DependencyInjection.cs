using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Pango.Application.Common.Mappings;
using System.Reflection;

namespace Pango.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.RegisterMappings();

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        return services;
    }
}
