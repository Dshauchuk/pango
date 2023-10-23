using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
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
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

public sealed partial class MainAppView : ViewBase
{
    private readonly IReadOnlyCollection<NavigationEntry> NavigationItems;
    private ResourceLoader _viewResourceLoader;

    public MainAppView()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<MainAppViewModel>();

        Loaded += MainAppView_Loaded;

        NavigationItems = new[]
        {
            new NavigationEntry(HomeItem, typeof(HomeView)),
            new NavigationEntry(PasswordsItem, typeof(PasswordsView))
        };

        _viewResourceLoader = ResourceLoader.GetForCurrentView();
    }

    private async void MainAppView_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel != null)
            await ViewModel.OnNavigatedToAsync(null);

        NavigationView.SelectedItem = HomeItem;
        NavigationFrame.Navigate(typeof(HomeView));

        ((Microsoft.UI.Xaml.Controls.NavigationViewItem)NavigationView.SettingsItem).Content = _viewResourceLoader.GetString("Settings");
    }

    private void NavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
    {
        AppView appView = AppView.MainAppView;
        if (NavigationItems.FirstOrDefault(item => item.Item == args.InvokedItemContainer)?.PageType is Type pageType)
        {
            NavigationFrame.Navigate(pageType);
            appView = pageType.GetCustomAttribute<AppViewAttribute>().View;
        }
        else if (args.IsSettingsInvoked)
        {
            NavigationFrame.Navigate(typeof(SettingsView));
            appView = AppView.Settings;
        }

        WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(appView)));
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
}
