using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Pango.Application;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Views;
using Pango.Infrastructure;
using Serilog;
using System;
using Windows.UI.Core.Preview;
using ApplicationBase = Microsoft.UI.Xaml.Application;

namespace Pango.Desktop.Uwp;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
sealed partial class App : ApplicationBase
{
    public MainWindow? CurrentWindow { get; private set; }
    public KeyboardHook? KeyboardHook { get; private set; }

    public static new App Current => (App)ApplicationBase.Current;
    
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        KeyboardHook = new KeyboardHook();

        this.UnhandledException += App_UnhandledException;
        SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += App_CloseRequested;
    }

    public event Action<string>? LoginSucceeded;
    public event Action? SignedOut;
    public event Action? AppCloseRequested;

    public static IHost Host { get; } = BuildHost();

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        Log.Logger?.Error(e.Exception, e?.Message ?? "Unhandled error");
    }

    private void App_CloseRequested(object? sender, SystemNavigationCloseRequestedPreviewEventArgs e)
    {
        AppCloseRequested?.Invoke();
    }

    private static IHost BuildHost()
    {
        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                _ = services
                    .AddLogging(configure =>
                    {
                        _ = configure
                            .SetMinimumLevel(LogLevel.Trace)
                            .AddSerilog()
                            .AddDebug();
                    })
                    .RegisterViewModels()
                    .AddApplicationServices()
                    .AddInfrastructureServices()
                    .AddAppServices()
                    .RegisterUIMappings()
                    .AddSingleton<MainWindow>();
            })
            .Build();
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="e">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        CurrentWindow = Host.Services.GetRequiredService<MainWindow>();
        CurrentWindow.Activate();
    }

    public void RaiseLoginSucceeded(string userName)
    {
        LoginSucceeded?.Invoke(userName);
    }

    public void RaiseSignedOut()
    {
        SignedOut?.Invoke();
    }
}
