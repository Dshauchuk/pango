using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Pango.Application;
using Pango.Desktop.Uwp.Views;
using Pango.Infrastructure;
using Serilog;
using Windows.ApplicationModel;
using ApplicationBase = Microsoft.UI.Xaml.Application;

namespace Pango.Desktop.Uwp;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
sealed partial class App : ApplicationBase
{
    private Window? _window;
    public static new App Current => (App)ApplicationBase.Current;
    
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        //this.Suspending += OnSuspending;

        this.UnhandledException += App_UnhandledException;
    }


    public static IHost Host { get; } = BuildHost();

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        Log.Logger?.Error(e.Exception, e?.Message ?? "Unhandled error");

        //WeakReferenceMessenger.Default.Send<InAppNotificationMessage>(new InAppNotificationMessage(e?.Message ?? "Unhandled error", AppNotificationType.Error));
    }

    private static IHost BuildHost()
    {
        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            //.UseSerilog((context, service, configuration) =>
            //{
            //    _ = configuration
            //        .MinimumLevel.Verbose()
            //        .WriteTo.File(
            //            "WinUI3Localizer.log",
            //            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
            //            rollingInterval: RollingInterval.Month);
            //})
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
                    .AddSingleton<MainWindow>()
                    //.AddSingleton<ILocalizer>(factory =>
                    //{
                    //    return new LocalizerBuilder()
                    //        .AddStringResourcesFolderForLanguageDictionaries(StringsFolderPath)
                    //        .SetLogger(Host.Services
                    //            .GetRequiredService<ILoggerFactory>()
                    //            .CreateLogger<Localizer>())
                    //        .SetOptions(options =>
                    //        {
                    //            options.DefaultLanguage = "ja";
                    //        })
                    //        .Build()
                    //        .GetAwaiter()
                    //        .GetResult();
                    //})
                    ;
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
        this._window = Host.Services.GetRequiredService<MainWindow>();
        this._window.Activate();

        //// Ensure the UI is initialized
        //if (Window.Current.Content is null)
        //{
        //    AddDependencyInjection();

        //    Window.Current.Content = new Shell();

        //    AppThemeHelper.Initialize();
        //    TitleBarHelper.StyleTitleBar();
        //    TitleBarHelper.ExpandViewIntoTitleBar();
        //}

        //// Enable the prelaunch if needed, and activate the window
        ////if (!e.PrelaunchActivated)
        ////{
        ////    CoreApplication.EnablePrelaunch(true);

        ////    Window.Current.Activate();
        ////}

        //var sb = new StringBuilder();
        //sb.AppendLine("System information: ")
        //    .AppendLine(string.Format("{0, -25} {1}", "Machine:", Environment.MachineName))
        //    .AppendLine(string.Format("{0, -25} {1}", "Windows (OS) Version:", Environment.OSVersion));

        //Log.Logger.Information(sb.ToString());
    }

    private void AddDependencyInjection()
    {
        var serviceCollection = new ServiceCollection()
            .RegisterViewModels()
            .AddApplicationServices()
            .AddInfrastructureServices()
            .AddAppServices();

        serviceCollection.RegisterUIMappings();

        // Register services
        //App.Host.Services.ConfigureServices(serviceCollection.BuildServiceProvider());
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
