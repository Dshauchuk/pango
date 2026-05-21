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
using System.Diagnostics;
using System.Runtime.InteropServices;
using ApplicationBase = Microsoft.UI.Xaml.Application;

namespace Pango.Desktop.Uwp;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
sealed partial class App : ApplicationBase
{
    /// <summary>
    /// Mutex for enforcing single-instance application behavior.
    /// </summary>
    private static Mutex? _mutex;

    /// <summary>
    /// Gets the current main window instance.
    /// </summary>
    public MainWindow? CurrentWindow { get; private set; }

    /// <summary>
    /// Gets the current application instance cast to App type.
    /// </summary>
    public static new App Current => (App)ApplicationBase.Current;

    /// <summary>
    /// Event triggered when user login succeeds, passing the username.
    /// </summary>
    public event Action<string>? LoginSucceeded;

    /// <summary>
    /// Event triggered when user signs out.
    /// </summary>
    public event Action? SignedOut;

    /// <summary>
    /// Gets the application's dependency injection host.
    /// </summary>
    public static IHost Host { get; } = BuildHost();

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class with single-instance enforcement.
    /// </summary>
    public App()
    {
        const string appName = "PangoApp_SingleInstance_Mutex";
        _mutex = new Mutex(true, appName, out bool createdNew);

        if (!createdNew)
        {
            BringExistingInstanceToFront();
            ApplicationBase.Current.Exit();
            return;
        }

        InitializeComponent();
        AppDomain.CurrentDomain.ProcessExit += (s, e) => DisposeHook();
        UnhandledException += App_UnhandledException;
    }

    /// <summary>
    /// Handles unhandled UI exceptions by logging and marking as handled.
    /// </summary>
    /// <param name="sender">The source of the exception event.</param>
    /// <param name="e">Event data containing the unhandled exception.</param>
    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        Log.Logger?.Error(e.Exception, e?.Message ?? "Unhandled error");
    }

    /// <summary>
    /// Builds and configures the application's dependency injection host with logging and services.
    /// </summary>
    /// <returns>Configured <see cref="IHost"/> instance.</returns>
    private static IHost BuildHost()
    {
        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                _ = services
                    .AddLogging(configure =>
                    {
                        _ = configure
                            .SetMinimumLevel(LogLevel.Information)
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
    /// Performs cleanup operations on application exit: stops host, resets messenger, and disposes mutex.
    /// </summary>
    public static void DisposeHook()
    {
        if (Host is IHost host)
        {
            host.Services.GetService<IHostApplicationLifetime>()?.StopApplication();
        }

        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Reset();

        try { _mutex?.Dispose(); } catch { }
        _mutex = null;
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.
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

    /// <summary>
    /// Raises the <see cref="LoginSucceeded"/> event with the provided username.
    /// </summary>
    /// <param name="userName">The name of the authenticated user.</param>
    public void RaiseLoginSucceeded(string userName)
    {
        LoginSucceeded?.Invoke(userName);
    }

    /// <summary>
    /// Raises the <see cref="SignedOut"/> event to notify subscribers of user logout.
    /// </summary>
    public void RaiseSignedOut()
    {
        SignedOut?.Invoke();
    }

    #region Single Instance Helpers

#pragma warning disable SYSLIB1054

    /// <summary>
    /// Sets the specified window to the foreground.
    /// </summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>
    /// Shows or restores the specified window.
    /// </summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

#pragma warning restore SYSLIB1054

    /// <summary>
    /// Constant for restoring a minimized window.
    /// </summary>
    private const int SW_RESTORE = 9;

    /// <summary>
    /// Brings an existing application instance to the foreground if one is already running.
    /// </summary>
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
