﻿using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Pango.Application.Common.Interfaces;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
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
using System.IO;
using Windows.Storage;

namespace Pango.Desktop.Uwp;

public static class DependencyInjection
{
    public static IServiceCollection RegisterUIMappings(this IServiceCollection services)
    {
        TypeAdapterConfig<PangoPasswordListItemDto, PangoExplorerItem>
        .NewConfig()
        .Map(dest => dest.Type, src => src.IsCatalog ? PangoExplorerItem.ExplorerItemType.Folder : PangoExplorerItem.ExplorerItemType.File);

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
            .AddSingleton<PasswordDetailsDialogViewModel>()
            .AddSingleton<ChangePasswordDialogViewModel>()
            .AddSingleton<ExportDialogViewModel>()
            .AddSingleton<ExportCompletedDialogViewModel>()
            .AddSingleton<ExportImportViewModel>()
            .AddSingleton<ImportDialogViewModel>()
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

        // DS
        // TODO: move to the config file
        services.AddSingleton<IAppOptions>((s) => new AppOptions(new FileOptions() { PasswordsPerFile = 2 }));

        return services;
    }
}
