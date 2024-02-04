using Microsoft.Extensions.DependencyInjection;
using Pango.Application.Common.Interfaces.Services;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Security;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Infrastructure.Services;
using Pango.Persistence;

namespace Pango.Desktop.Uwp;

public static class DependencyInjection
{
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
            .AddTransient<SignInViewModel>();

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordVault, AppPasswordVault>();
        services.AddScoped<IAppDomainProvider, AppDomainProvider>();
        services.AddScoped<IPasswordHashProvider, PasswordHashProvider>();
        services.AddScoped<IUserContextProvider, UserContextProvider>();

        return services;
    }
}
