using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Pango.Application.Common.Interfaces.Services;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Navigation;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using Serilog;
using System.Reflection;

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// Represents the main shell view containing the NavigationView sidebar and the main content Frame.
/// </summary>
[AppView(AppView.MainAppView)]
public sealed partial class MainAppView : ViewBase
{
    private Type? _initialView;
    private readonly IReadOnlyCollection<NavigationEntry> NavigationItems;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainAppView"/> class.
    /// </summary>
    /// <param name="initialView">Optional view type to navigate to on load.</param>
    public MainAppView(Type? initialView = null)
    {
        InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<MainAppViewModel>();

        _initialView = initialView;

        Loaded += MainAppView_LoadedAsync;
        Unloaded += MainAppView_Unloaded;

        var navService = App.Host.Services.GetRequiredService<INavigationService>();
        (navService as NavigationService)?.SetFrame(NavigationFrame);

        NavigationItems =
        [
            new NavigationEntry(HomeItem, typeof(HomeView)),
            new NavigationEntry(PasswordsItem, typeof(PasswordsView)),
            new NavigationEntry(UserItem, typeof(UserView)),
            new NavigationEntry(ExportImportItem, typeof(ExportImportView)),
            new NavigationEntry(GeneratePasswordItem, typeof(GeneratePasswordView)),
            new NavigationEntry(CustomSettingsItem, typeof(SettingsView))
        ];

        Log.Logger?.Debug("MainAppView initialized with initial view: {InitialView}", initialView?.Name ?? "null");
    }

    #region Event Handlers & Navigation

    /// <summary>
    /// Handles the Loaded event asynchronously: initializes ViewModel and navigates to initial page.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private async void MainAppView_LoadedAsync(object sender, RoutedEventArgs e)
    {
        Log.Logger?.Debug("MainAppView loaded");

        if (ViewModel != null)
        {
            await ViewModel.OnNavigatedToAsync(null);
            Log.Logger?.Debug("ViewModel.OnNavigatedToAsync completed");
        }

        NavigateToInitialPage();
    }

    /// <summary>
    /// Cleans up event subscriptions when the view unloads to prevent memory leaks.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments.</param>
    private void MainAppView_Unloaded(object sender, RoutedEventArgs e)
    {
        Log.Logger?.Debug("MainAppView unloaded: cleaning up event handlers");

        Loaded -= MainAppView_LoadedAsync;
        Unloaded -= MainAppView_Unloaded;
    }

    /// <summary>
    /// Handles item clicks in the NavigationView and requests navigation via messenger.
    /// </summary>
    /// <param name="sender">The NavigationView that raised the event.</param>
    /// <param name="args">Event data containing the invoked item container.</param>
    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        Log.Logger?.Debug("NavigationView item invoked: {Item}", args.InvokedItem?.GetType().Name ?? "null");

        AppView appView;

        if (NavigationItems.FirstOrDefault(item => item.Item == args.InvokedItemContainer)?.PageType is Type pageType)
        {
            appView = pageType.GetCustomAttribute<AppViewAttribute>()?.View
                      ?? throw new InvalidCastException($"Page {pageType.Name} MUST have {nameof(AppViewAttribute)}");
        }
        else
        {
            Log.Logger?.Warning("NavigationView_ItemInvoked: invoked item not found in NavigationItems");
            return;
        }

        WeakReferenceMessenger.Default.Send(
            new NavigationRequestedMessage(
                new Mvvm.Models.NavigationParameters(appView, AppView.MainAppView)));

        Log.Logger?.Information("Navigation requested to {AppView}", appView);
    }

    /// <summary>
    /// Keeps the NavigationView selection in sync with the Frame's current page after navigation.
    /// </summary>
    /// <param name="sender">The Frame that raised the event.</param>
    /// <param name="e">Navigation event arguments.</param>
    private void NavigationFrame_Navigated(object sender, NavigationEventArgs e)
    {
        Log.Logger?.Debug("NavigationFrame navigated to {PageType}", e.SourcePageType?.Name ?? "null");

        var frame = (Frame)sender;
        frame.BackStack.Clear();

        NavigationView.IsBackEnabled = false;
        var navigatedPageType = e.SourcePageType;

        NavigationView.SelectedItem = NavigationItems
            .FirstOrDefault(item => item.PageType == navigatedPageType)?.Item;

        Log.Logger?.Debug("NavigationView selection updated");
    }

    /// <summary>
    /// Handles the global back button request inside the navigation view.
    /// </summary>
    /// <param name="sender">The NavigationView that raised the event.</param>
    /// <param name="args">Back requested event arguments.</param>
    private void NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        Log.Logger?.Debug("NavigationView back requested");

        if (NavigationFrame.CanGoBack)
        {
            NavigationFrame.GoBack();
            Log.Logger?.Information("NavigationFrame navigated back");
        }
        else
        {
            Log.Logger?.Debug("NavigationFrame cannot go back");
        }
    }

    /// <summary>
    /// Navigates the user to the initial view page (Home by default) via messenger.
    /// </summary>
    private void NavigateToInitialPage()
    {
        Log.Logger?.Debug("NavigateToInitialPage called");

        AppView targetView;

        if (_initialView is not null)
        {
            targetView = _initialView.GetCustomAttribute<AppViewAttribute>()?.View ?? AppView.Home;
            Log.Logger?.Debug("Initial view attribute resolved to {TargetView}", targetView);
        }
        else
        {
            targetView = AppView.Home;
            Log.Logger?.Debug("Using default initial view: Home");
        }

        _initialView = null;

        WeakReferenceMessenger.Default.Send(
            new NavigationRequestedMessage(
                new Mvvm.Models.NavigationParameters(targetView, AppView.MainAppView)));

        Log.Logger?.Information("Initial navigation requested to {TargetView}", targetView);
    }

    #endregion

    #region Hover Animations for Navigation Icons

    /// <summary>
    /// Animates the specified property of a UI element's CompositeTransform using a storyboard.
    /// </summary>
    /// <param name="target">The UI element containing the CompositeTransform to animate.</param>
    /// <param name="propertyPath">The name of the property to animate (e.g., "TranslateY", "Rotation").</param>
    /// <param name="toValue">The final value the animation should reach.</param>
    private static void AnimateTransform(UIElement target, string propertyPath, double toValue)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            To = toValue,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(animation, target.RenderTransform);
        Storyboard.SetTargetProperty(animation, propertyPath);

        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    /// <summary>
    /// Handles pointer enter on Home icon: triggers slight upward jump animation.
    /// </summary>
    private void Home_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        Log.Logger?.Debug("Home icon pointer entered");
        AnimateTransform(HomeIcon, "TranslateY", -3);
    }

    /// <summary>
    /// Handles pointer exit on Home icon: resets jump animation.
    /// </summary>
    private void Home_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Log.Logger?.Debug("Home icon pointer exited");
        AnimateTransform(HomeIcon, "TranslateY", 0);
    }

    /// <summary>
    /// Handles pointer enter on Passwords icon: triggers slight upward jump animation.
    /// </summary>
    private void Passwords_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        Log.Logger?.Debug("Passwords icon pointer entered");
        AnimateTransform(PasswordsIcon, "TranslateY", -3);
    }

    /// <summary>
    /// Handles pointer exit on Passwords icon: resets jump animation.
    /// </summary>
    private void Passwords_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Log.Logger?.Debug("Passwords icon pointer exited");
        AnimateTransform(PasswordsIcon, "TranslateY", 0);
    }

    /// <summary>
    /// Handles pointer enter on Users icon: triggers scale-up animation.
    /// </summary>
    private void Users_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        Log.Logger?.Debug("Users icon pointer entered");
        AnimateTransform(UsersIcon, "ScaleX", 1.15);
        AnimateTransform(UsersIcon, "ScaleY", 1.15);
    }

    /// <summary>
    /// Handles pointer exit on Users icon: resets scale animation.
    /// </summary>
    private void Users_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Log.Logger?.Debug("Users icon pointer exited");
        AnimateTransform(UsersIcon, "ScaleX", 1.0);
        AnimateTransform(UsersIcon, "ScaleY", 1.0);
    }

    /// <summary>
    /// Handles pointer enter on Export icon: triggers slight rightward shift animation.
    /// </summary>
    private void Export_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        Log.Logger?.Debug("Export icon pointer entered");
        AnimateTransform(ExportIcon, "TranslateX", 5);
    }

    /// <summary>
    /// Handles pointer exit on Export icon: resets shift animation.
    /// </summary>
    private void Export_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Log.Logger?.Debug("Export icon pointer exited");
        AnimateTransform(ExportIcon, "TranslateX", 0);
    }

    /// <summary>
    /// Handles pointer enter on Generator icon: triggers slight left tilt animation.
    /// </summary>
    private void Generator_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        Log.Logger?.Debug("Generator icon pointer entered");
        AnimateTransform(GeneratorIcon, "Rotation", -15);
    }

    /// <summary>
    /// Handles pointer exit on Generator icon: resets rotation animation.
    /// </summary>
    private void Generator_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Log.Logger?.Debug("Generator icon pointer exited");
        AnimateTransform(GeneratorIcon, "Rotation", 0);
    }

    /// <summary>
    /// Handles pointer enter on Settings icon: triggers 90° rotation animation (gear-like effect).
    /// </summary>
    private void Settings_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        Log.Logger?.Debug("Settings icon pointer entered");
        AnimateTransform(SettingsIcon, "Rotation", 90);
    }

    /// <summary>
    /// Handles pointer exit on Settings icon: resets rotation animation.
    /// </summary>
    private void Settings_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Log.Logger?.Debug("Settings icon pointer exited");
        AnimateTransform(SettingsIcon, "Rotation", 0);
    }

    #endregion
}
