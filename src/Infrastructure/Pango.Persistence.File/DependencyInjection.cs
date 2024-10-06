using Microsoft.Extensions.DependencyInjection;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;

namespace Pango.Persistence.File;

public static class DependencyInjection
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services)
    {
        services.AddScoped<IUserStorageManager, UserFileStorageManager>();
        services.AddScoped<IPasswordRepository, PasswordFileRepository>();
        services.AddScoped<IRepositoryContextFactory, FileRepositoryContextFactory>();
        services.AddSingleton<IDataImporter, PangoFileDataImporter>();
        services.AddSingleton<IDataExporter, PangoFileDataExporter>();

        return services;
    }
}
