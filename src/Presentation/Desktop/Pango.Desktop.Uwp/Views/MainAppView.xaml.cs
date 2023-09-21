using CommunityToolkit.Mvvm.DependencyInjection;
using Pango.Desktop.Uwp.Core.Navigation;
using Pango.Desktop.Uwp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Pango.Desktop.Uwp.Views;

public sealed partial class MainAppView : UserControl
{
    private readonly IReadOnlyCollection<NavigationEntry> NavigationItems;

    public MainAppView()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<MainAppViewModel>();

        Loaded += MainAppView_Loaded;

        NavigationItems = new[]
        {
            new NavigationEntry(HomeItem, typeof(HomeView)),
            new NavigationEntry(PasswordsItem, typeof(PasswordsView)),
            new NavigationEntry(SettingsItem, typeof(SettingsView))
        };
    }

    private void MainAppView_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        //if (ViewModel != null)
        //    await ViewModel.OnNavigatedToAsync(null);

        //NavigationView.SelectedItem = HomeItem;

        //NavigationFrame.Navigate(typeof(HomePage));
    }

    private void NavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
    {
        if (NavigationItems.FirstOrDefault(item => item.Item == args.InvokedItemContainer)?.PageType is Type pageType)
        {
            NavigationFrame.Navigate(pageType);
        }
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
