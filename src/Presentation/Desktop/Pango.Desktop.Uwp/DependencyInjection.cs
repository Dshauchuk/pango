using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using System;

namespace Pango.Desktop.Uwp;

public class TmpLogger : ILogger
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {

    }
}

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
            .AddTransient<ShellViewModel>()
            .AddTransient<HomeViewModel>()
            .AddTransient<MainAppViewModel>()
            .AddTransient<EditUserViewModel>()
            .AddTransient<EditPasswordViewModel>()
            .AddTransient<SettingsViewModel>()
            .AddTransient<PasswordsViewModel>()
            .AddTransient<SignInViewModel>()
            .AddTransient<NewPasswordCatalogDialogViewModel>()
            .AddTransient<UserViewModel>();

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordVault, AppPasswordVault>();
        services.AddScoped<IAppDomainProvider, AppDomainProvider>();
        services.AddScoped<IPasswordHashProvider, PasswordHashProvider>();
        services.AddScoped<IUserContextProvider, UserContextProvider>();
        services.AddScoped<IAppUserProvider, AppUserProvider>();
        services.AddScoped<IDialogService, DialogService>();
        services.AddScoped<ILogger, TmpLogger>();

        // DS
        // TODO: move to the config file
        services.AddSingleton<IAppOptions>((s) => new AppOptions() { FileOptions = new FileOptions() { PasswordsPerFile = 2 } });

        return services;
    }
}
