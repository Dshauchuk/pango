using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Pango.Application.Common.Interfaces;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Desktop.Uwp.Core.Navigation;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Core.Utility.Contracts;
using Pango.Desktop.Uwp.Dialogs;
using Pango.Desktop.Uwp.Dialogs.ViewModels;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Security;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Infrastructure.Services;
using Pango.Persistence;
using Pango.Persistence.File;
using Serilog;
using Windows.Storage;

namespace Pango.Desktop.Uwp;

public static class DependencyInjection
{
    public static IServiceCollection RegisterUIMappings(this IServiceCollection services)
    {
        TypeAdapterConfig<PangoPasswordListItemDto, PangoExplorerItem>
        .NewConfig()
        .Map(dest => dest.Type, src => src.IsCatalog ? PangoExplorerItem.ExplorerItemType.Folder : PangoExplorerItem.ExplorerItemType.File)
        .Map(dest => dest.ExpirationDate, src => GetExpirationDateFromProperties(src.Properties))
        .Map(dest => dest.IsStar, src => src.Star);

        return services;
    }

    private static DateTimeOffset? GetExpirationDateFromProperties(System.Collections.Generic.Dictionary<string, string> properties)
    {
        if (properties != null && properties.TryGetValue(Application.Common.PasswordProperties.ExpirationDate, out var dateStr))
        {
            if (DateTimeOffset.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedDate))
                return parsedDate;

            if (DateTimeOffset.TryParse(dateStr, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.RoundtripKind, out parsedDate))
                return parsedDate;
        }
        return null;
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
            .AddTransient<EditPasswordCatalogDialogViewModel>()
            .AddTransient<PasswordDetailsDialogViewModel>()
            .AddTransient<ChangePasswordDialogViewModel>()
            .AddTransient<ExportDialogViewModel>()
            .AddTransient<ExportCompletedDialogViewModel>()
            .AddSingleton<ExportImportViewModel>()
            .AddTransient<ImportDialogViewModel>()
            .AddSingleton<UserViewModel>()
            .AddSingleton<GeneratePasswordViewModel>()
            .AddTransient<GeneratePasswordDialogViewModel>();

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
            .MinimumLevel.Debug()
            .CreateLogger();

        services.AddScoped<IPasswordVault, AppPasswordVault>();
        services.AddScoped<IAppDomainProvider, AppDomainProvider>();
        services.AddScoped<IPasswordHashProvider, PasswordHashProvider>();
        services.AddScoped<IUserContextProvider, UserContextProvider>();
        services.AddScoped<IUserStorageManager, UserFileStorageManager>();
        services.AddScoped<IAppUserProvider, AppUserProvider>();
        services.AddScoped<IDialogService, DialogService>();
        services.AddScoped<IAppMetaService, AppMetaService>();
        services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

        services.AddSingleton<IAppIdleService, AppIdleService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IPasswordGeneratorSettingsService, PasswordGeneratorSettingsService>();

        // DS
        // TODO: move to the config file
        services.AddSingleton<IAppOptions>((s) => new AppOptions(new FileOptions() { PasswordsPerFile = 20 }));

        return services;
    }
}
