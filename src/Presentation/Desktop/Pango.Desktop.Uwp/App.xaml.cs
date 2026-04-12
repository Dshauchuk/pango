using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Pango.Application;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Views;
using Pango.Infrastructure;
using Pango.Persistence;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ApplicationBase = Microsoft.UI.Xaml.Application;

namespace Pango.Desktop.Uwp;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
sealed partial class App : ApplicationBase
{
    private static Mutex? _mutex;
    public MainWindow? CurrentWindow { get; private set; }
    public KeyboardHook? KeyboardHook { get; private set; }

    public static new App Current => (App)ApplicationBase.Current;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        const string appName = "PangoApp_SingleInstance_Mutex";
        _mutex = new Mutex(true, appName, out bool createdNew);

        if (!createdNew)
        {
            BringExistingInstanceToFront();
            Environment.Exit(0);
            return;
        }

        InitializeComponent();
        KeyboardHook = new KeyboardHook();
        UnhandledException += App_UnhandledException;
    }

    public event Action<string>? LoginSucceeded;
    public event Action? SignedOut;

    public static IHost Host { get; } = BuildHost();

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        Log.Logger?.Error(e.Exception, e?.Message ?? "Unhandled error");
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
                            .AddDebug()
                            .AddEventLog(settings =>
                            {
                                settings.SourceName = "PangoApp";
                                settings.LogName = "Application";
                            });
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
    protected override async void OnLaunched(LaunchActivatedEventArgs e)
    {
        var appDomainProvider = Host.Services.GetRequiredService<IAppDomainProvider>();
        if (appDomainProvider is AppDomainProvider provider)
        {
            await provider.InitializeAsync();
        }

        CurrentWindow = Host.Services.GetRequiredService<MainWindow>();
        CurrentWindow?.Activate();
    }

    public void RaiseLoginSucceeded(string userName)
    {
        LoginSucceeded?.Invoke(userName);
    }

    public void RaiseSignedOut()
    {
        SignedOut?.Invoke();
    }

    #region Single Instance Helpers

#pragma warning disable SYSLIB1054
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
#pragma warning restore SYSLIB1054

    private const int SW_RESTORE = 9;

    private static void BringExistingInstanceToFront()
    {
        var currentProcess = Process.GetCurrentProcess();
        var existingProcess = Process.GetProcessesByName(currentProcess.ProcessName)
            .FirstOrDefault(p => p.Id != currentProcess.Id);

        if (existingProcess != null)
        {
            IntPtr handle = existingProcess.MainWindowHandle;
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, SW_RESTORE);
                SetForegroundWindow(handle);
            }
        }
    }

    #endregion
}
