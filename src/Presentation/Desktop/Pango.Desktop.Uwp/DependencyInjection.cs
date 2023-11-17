using Microsoft.Extensions.DependencyInjection;
using Pango.Desktop.Uwp.ViewModels;

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
}
