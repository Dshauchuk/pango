using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pango.Application;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Views;
using Pango.Infrastructure;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using ApplicationBase = Windows.UI.Xaml.Application;

namespace Pango.Desktop.Uwp;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
sealed partial class App : ApplicationBase
{
    private ILogger _logger;
    public static new App Current => (App)ApplicationBase.Current;
    
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        this.Suspending += OnSuspending;

        this.UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        _logger?.LogError(e.Exception, e?.Message ?? "Unhandled error");

        //WeakReferenceMessenger.Default.Send<InAppNotificationMessage>(new InAppNotificationMessage(e?.Message ?? "Unhandled error", AppNotificationType.Error));
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="e">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        // Ensure the UI is initialized
        if (Window.Current.Content is null)
        {
            AddDependencyInjection();

            Window.Current.Content = new Shell();

            AppThemeHelper.Initialize();
            TitleBarHelper.StyleTitleBar();
            TitleBarHelper.ExpandViewIntoTitleBar();
        }

        // Enable the prelaunch if needed, and activate the window
        if (!e.PrelaunchActivated)
        {
            CoreApplication.EnablePrelaunch(true);

            Window.Current.Activate();
        }
    }

    private void AddDependencyInjection()
    {
        var serviceCollection = new ServiceCollection()
            .RegisterViewModels()
            .AddApplicationServices()
            .AddInfrastructureServices()
            .AddAppServices();

        // Register services
        Ioc.Default.ConfigureServices(serviceCollection.BuildServiceProvider());
    }

    /// <summary>
    /// Invoked when application execution is being suspended.  Application state is saved
    /// without knowing whether the application will be terminated or resumed with the contents
    /// of memory still intact.
    /// </summary>
    /// <param name="sender">The source of the suspend request.</param>
    /// <param name="e">Details about the suspend request.</param>
    private void OnSuspending(object sender, SuspendingEventArgs e)
    {
        var deferral = e.SuspendingOperation.GetDeferral();

        //TODO: Save application state and stop any background activity
        deferral.Complete();
    }
}
