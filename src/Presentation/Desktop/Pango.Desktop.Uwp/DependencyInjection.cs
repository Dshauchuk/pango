using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Pango.Application.Common.Interfaces;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Dialogs;
using Pango.Desktop.Uwp.Dialogs.ViewModels;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Security;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Infrastructure.Services;
using Pango.Persistence;
using Serilog;
using System.IO;
using Windows.Storage;

namespace Pango.Desktop.Uwp;

//public class TmpLogger : ILogger
//{
//    public IDisposable BeginScope<TState>(TState state) where TState : notnull
//    {
//        return null;
//    }

//    public bool IsEnabled(LogLevel logLevel)
//    {
//        return true;
//    }

//    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
//    {

//    }
//}

public class AppOptions : IAppOptions
{
    public IFileOptions FileOptions { get; set; }
}

public class FileOptions : IFileOptions
{
    public int PasswordsPerFile { get; set; }
}

public static class DependencyInjection
{
    public static IServiceCollection RegisterUIMappings(this IServiceCollection services)
    {
        TypeAdapterConfig<PangoPasswordListItemDto, PasswordExplorerItem>
        .NewConfig()
        .Map(dest => dest.Type, src => src.IsCatalog ? PasswordExplorerItem.ExplorerItemType.Folder : PasswordExplorerItem.ExplorerItemType.File);

        return services;
    }

    public static IServiceCollection RegisterViewModels(this IServiceCollection services)
    {
        services
            .AddSingleton<ShellViewModel>()
            .AddSingleton<HomeViewModel>()
            .AddSingleton<MainAppViewModel>()
            .AddSingleton<EditUserViewModel>()
            .AddSingleton<EditPasswordViewModel>()
            .AddSingleton<SettingsViewModel>()
            .AddSingleton<PasswordsViewModel>()
            .AddSingleton<SignInViewModel>()
            .AddSingleton<EditPasswordCatalogDialogViewModel>()
            .AddSingleton<UserViewModel>();

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        string logFilePath = Path.Combine(Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs/log.txt"));
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(logFilePath,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
            .CreateLogger();

        services.AddScoped<IPasswordVault, AppPasswordVault>();
        services.AddScoped<IAppDomainProvider, AppDomainProvider>();
        services.AddScoped<IPasswordHashProvider, PasswordHashProvider>();
        services.AddScoped<IUserContextProvider, UserContextProvider>();
        services.AddScoped<IAppUserProvider, AppUserProvider>();
        services.AddScoped<IDialogService, DialogService>();
        services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

        // DS
        // TODO: move to the config file
        services.AddSingleton<IAppOptions>((s) => new AppOptions() { FileOptions = new FileOptions() { PasswordsPerFile = 2 } });

        return services;
    }
}
