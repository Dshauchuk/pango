using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
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
    private Type _initialView;
    private readonly IReadOnlyCollection<NavigationEntry> NavigationItems;
    private ResourceLoader _viewResourceLoader;

    public MainAppView(Type initialView = null)
    {
        this.InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<MainAppViewModel>();
        _initialView = initialView;

        Loaded += MainAppView_Loaded;

        NavigationItems = new[]
        {
            new NavigationEntry(HomeItem, typeof(HomeView)),
            new NavigationEntry(PasswordsItem, typeof(PasswordsView)),
            new NavigationEntry(UserItem, typeof(UserView))
        };

        _viewResourceLoader = new ResourceLoader();
    }

    private async void MainAppView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel != null)
            await ViewModel.OnNavigatedToAsync(null);

        NavigateToInitialPage();

        ((Microsoft.UI.Xaml.Controls.NavigationViewItem)NavigationView.SettingsItem).Content = _viewResourceLoader.GetString("Settings");
    }

    private void NavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
    {
        AppView appView = AppView.MainAppView;
        if (NavigationItems.FirstOrDefault(item => item.Item == args.InvokedItemContainer)?.PageType is Type pageType)
        {
            NavigationFrame.Navigate(pageType);
            appView = pageType.GetCustomAttribute<AppViewAttribute>()?.View ?? throw new InvalidCastException($"Page {pageType.Name} MUST have {nameof(AppViewAttribute)}");
        }
        else if (args.IsSettingsInvoked)
        {
            NavigationFrame.Navigate(typeof(SettingsView));
            appView = AppView.Settings;
        }

        WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(appView, AppView.MainAppView)));
    }

    private void NavigationFrame_Navigated(object sender, NavigationEventArgs e)
    {
        NavigationView.IsBackEnabled = ((Frame)sender).BackStackDepth > 0;
    }

    private void NavigationView_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs args)
    {
        if (NavigationFrame.BackStack.LastOrDefault() is PageStackEntry entry)
        {
            NavigationView.SelectedItem = NavigationItems.First(item => item.PageType == entry.SourcePageType).Item;

            NavigationFrame.GoBack();
        }
    }

    /// <summary>
    /// Navigates the User to the <see cref="_initialView"/> page if it specified. If <see cref="_initialView"/> does not specified or incorrect - navigates to the default initial page
    /// </summary>
    private void NavigateToInitialPage()
    {
        if (_initialView is not null)
        {
            if (_initialView == typeof(SettingsView))
            {
                NavigationView.SelectedItem = NavigationView.SettingsItem;
            }
            else
            {
                NavigationView.SelectedItem = NavigationItems.FirstOrDefault(item => item.PageType == _initialView);
            }
        }
        if (NavigationView.SelectedItem is null)
        {
            NavigationView.SelectedItem = HomeItem;
            NavigationFrame.Navigate(typeof(HomeView));
        }
        else
        {
            NavigationFrame.Navigate(_initialView);
        }

        _initialView = null;
    }
}
