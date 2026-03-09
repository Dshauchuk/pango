using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
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
using Windows.ApplicationModel.Resources;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236  

namespace Pango.Desktop.Uwp.Views;

[AppView(AppView.MainAppView)]
public sealed partial class MainAppView : ViewBase
{
    /// <summary>
    /// Contains Type of a view to which User should be redirected, when the View will be loaded
    /// </summary>
    private Type? _initialView;
    private readonly IReadOnlyCollection<NavigationEntry> NavigationItems;
    private readonly ResourceLoader _viewResourceLoader;

    public MainAppView(Type? initialView = null)
    {
        InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<MainAppViewModel>();

        _initialView = initialView;
        _viewResourceLoader = new ResourceLoader();

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
            new NavigationEntry(GeneratePasswordItem, typeof(GeneratePasswordView))
        ];
    }

    #region Event Handlers

    private async void MainAppView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs routedEventArgs)
    {
        OnNavigatedTo(null);

        if (ViewModel != null)
        {
            await ViewModel.OnNavigatedToAsync(null);
        }

        NavigateToInitialPage();

        // Set localized string for Settings
        NavigationViewItem settingsItem = (NavigationViewItem)NavigationView.SettingsItem;
        settingsItem.Content = _viewResourceLoader.GetString("Settings");
        settingsItem.IsTabStop = false;
    }

    private void MainAppView_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs routedEventArgs)
    {
        Loaded -= MainAppView_Loaded;
        Unloaded -= MainAppView_Unloaded;
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs invokedEventArgs)
    {
        AppView appView = AppView.MainAppView;
        NavigationEntry? navigationEntry = NavigationItems.FirstOrDefault(item => item.Item == invokedEventArgs.InvokedItemContainer);

        if (navigationEntry != null && navigationEntry.PageType is Type pageType)
        {
            if (NavigationFrame.CurrentSourcePageType == pageType)
            {
                return;
            }

            NavigationFrame.Navigate(pageType);

            NavigationFrame.BackStack.Clear();

            AppViewAttribute? viewAttribute = pageType.GetCustomAttribute<AppViewAttribute>();
            if (viewAttribute != null)
            {
                appView = viewAttribute.View;
            }
            else
            {
                throw new InvalidCastException($"Page {pageType.Name} MUST have {nameof(AppViewAttribute)}");
            }
        }
        else if (invokedEventArgs.IsSettingsInvoked)
        {
            if (NavigationFrame.CurrentSourcePageType != typeof(SettingsView))
            {
                NavigationFrame.Navigate(typeof(SettingsView));
                NavigationFrame.BackStack.Clear();
                appView = AppView.Settings;
            }
        }

        WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(appView, AppView.MainAppView)));

        NavigationView.IsBackEnabled = false;
    }

    private void NavigationFrame_Navigated(object sender, NavigationEventArgs navigationEventArgs)
    {
        NavigationView.IsBackEnabled = ((Frame)sender).BackStackDepth > 0;

        var navigatedPageType = navigationEventArgs.SourcePageType;

        if(navigatedPageType == typeof(SettingsView))
        {
            NavigationView.SelectedItem = NavigationView.SettingsItem;
        }
        else
        {
            NavigationView.SelectedItem = NavigationItems.FirstOrDefault(item => item.PageType == navigatedPageType)?.Item;
        }
    }

    private void NavigationView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs backRequestedEventArgs)
    {
        PageStackEntry? lastEntry = NavigationFrame.BackStack.LastOrDefault();
        if (lastEntry is PageStackEntry entry)
        {
            NavigationEntry? navItem = NavigationItems.FirstOrDefault(item => item.PageType == entry.SourcePageType);
            if (navItem != null)
            {
                NavigationView.SelectedItem = navItem.Item;
            }

            NavigationFrame.GoBack();
        }
    }

    #endregion

    /// <summary>
    /// Navigates the User to the <see cref="_initialView"/> page if it specified. If <see cref="_initialView"/> does not specified or incorrect - navigates to the default initial page
    /// </summary>
    private void NavigateToInitialPage()
    {
        AppView targetView;

        if (_initialView is not null)
        {
            if (_initialView == typeof(SettingsView))
            {
                targetView = AppView.Settings;
            }
            else
            {
                NavigationEntry? targetItem = NavigationItems.FirstOrDefault(item => item.PageType == _initialView);
                if (targetItem != null)
                {
                    NavigationView.SelectedItem = targetItem.Item;
                }
            }
        }

        if (NavigationView.SelectedItem is null)
        {
            NavigationView.SelectedItem = HomeItem;
            if (NavigationFrame.CurrentSourcePageType != typeof(HomeView))
            {
                NavigationFrame.Navigate(typeof(HomeView));
                NavigationFrame.BackStack.Clear();
            }
        }
        else
        {
            if (_initialView != null && NavigationFrame.CurrentSourcePageType != _initialView)
            {
                NavigationFrame.Navigate(_initialView);
                NavigationFrame.BackStack.Clear();
            }
        }

        _initialView = null;
    }
    

    
}
