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
using System;
using System.Collections.Generic;
using System.Linq;
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

        Loaded += MainAppView_Loaded;
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
    }

    #region Event Handlers & Navigation

    /// <summary>
    /// Handles the Loaded event. Performs initial navigation.
    /// </summary>
    private async void MainAppView_Loaded(object sender, RoutedEventArgs e)
    {
        OnNavigatedTo(null);

        if (ViewModel != null)
        {
            await ViewModel.OnNavigatedToAsync(null);
        }

        NavigateToInitialPage();
    }

    /// <summary>
    /// Cleans up event subscriptions when the view unloads.
    /// </summary>
    private void MainAppView_Unloaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainAppView_Loaded;
        Unloaded -= MainAppView_Unloaded;
    }

    /// <summary>
    /// Handles item clicks in the NavigationView and requests navigation.
    /// </summary>
    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        AppView appView;

        if (NavigationItems.FirstOrDefault(item => item.Item == args.InvokedItemContainer)?.PageType is Type pageType)
        {
            appView = pageType.GetCustomAttribute<AppViewAttribute>()?.View
                      ?? throw new InvalidCastException($"Page {pageType.Name} MUST have {nameof(AppViewAttribute)}");
        }
        else
        {
            return;
        }

        WeakReferenceMessenger.Default.Send(
            new NavigationRequstedMessage(
                new Mvvm.Models.NavigationParameters(appView, AppView.MainAppView)));
    }

    /// <summary>
    /// Keeps the NavigationView selection in sync with the Frame's current page.
    /// </summary>
    private void NavigationFrame_Navigated(object sender, NavigationEventArgs e)
    {
        NavigationView.IsBackEnabled = ((Frame)sender).BackStackDepth > 0;
        var navigatedPageType = e.SourcePageType;

        NavigationView.SelectedItem = NavigationItems
            .FirstOrDefault(item => item.PageType == navigatedPageType)?.Item;
    }

    /// <summary>
    /// Handles the global back button request inside the navigation view.
    /// </summary>
    private void NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (NavigationFrame.CanGoBack)
        {
            NavigationFrame.GoBack();
        }
    }

    /// <summary>
    /// Navigates the user to the initial view page (Home by default).
    /// </summary>
    private void NavigateToInitialPage()
    {
        AppView targetView;

        if (_initialView is not null)
        {
            targetView = _initialView.GetCustomAttribute<AppViewAttribute>()?.View ?? AppView.Home;
        }
        else
        {
            targetView = AppView.Home;
        }

        _initialView = null;

        WeakReferenceMessenger.Default.Send(
            new NavigationRequstedMessage(
                new Mvvm.Models.NavigationParameters(targetView, AppView.MainAppView)));
    }

    #endregion

    #region Hover Animations for Navigation Icons

    /// <summary>
    /// Animates the specific property of a UI element's transform.
    /// </summary>
    /// <param name="target">The UI element containing the CompositeTransform.</param>
    /// <param name="propertyPath">The name of the property to animate (e.g., "TranslateY", "Rotation").</param>
    /// <param name="toValue">The final value of the animation.</param>
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

    // Home Icon: Slight jump
    private void Home_PointerEntered(object sender, PointerRoutedEventArgs e) => AnimateTransform(HomeIcon, "TranslateY", -3);
    private void Home_PointerExited(object sender, PointerRoutedEventArgs e) => AnimateTransform(HomeIcon, "TranslateY", 0);

    // Passwords Icon: Slight jump
    private void Passwords_PointerEntered(object sender, PointerRoutedEventArgs e) => AnimateTransform(PasswordsIcon, "TranslateY", -3);
    private void Passwords_PointerExited(object sender, PointerRoutedEventArgs e) => AnimateTransform(PasswordsIcon, "TranslateY", 0);

    // Users Icon: Scales up slightly
    private void Users_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        AnimateTransform(UsersIcon, "ScaleX", 1.15);
        AnimateTransform(UsersIcon, "ScaleY", 1.15);
    }
    private void Users_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        AnimateTransform(UsersIcon, "ScaleX", 1.0);
        AnimateTransform(UsersIcon, "ScaleY", 1.0);
    }

    // Export/Import Icon: Shifts right
    private void Export_PointerEntered(object sender, PointerRoutedEventArgs e) => AnimateTransform(ExportIcon, "TranslateX", 5);
    private void Export_PointerExited(object sender, PointerRoutedEventArgs e) => AnimateTransform(ExportIcon, "TranslateX", 0);

    // Generator Icon: Tilts to the left
    private void Generator_PointerEntered(object sender, PointerRoutedEventArgs e) => AnimateTransform(GeneratorIcon, "Rotation", -15);
    private void Generator_PointerExited(object sender, PointerRoutedEventArgs e) => AnimateTransform(GeneratorIcon, "Rotation", 0);

    // Settings Icon: Spins around like a gear
    private void Settings_PointerEntered(object sender, PointerRoutedEventArgs e) => AnimateTransform(SettingsIcon, "Rotation", 90);
    private void Settings_PointerExited(object sender, PointerRoutedEventArgs e) => AnimateTransform(SettingsIcon, "Rotation", 0);

    #endregion
}