using Pango.Desktop.Uwp.Core.Navigation;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Windows.Globalization;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Shell : UserControl
{
    #region Fields

    private readonly IReadOnlyCollection<NavigationEntry> NavigationItems;
    private bool _isWindowActive;

    #endregion

    internal ShellViewModel ViewModel => (ShellViewModel)DataContext;

    public Shell()
    {
        this.InitializeComponent();

        CultureInfo ci = new CultureInfo("be-BY");
        Thread.CurrentThread.CurrentCulture = ci;
        Thread.CurrentThread.CurrentUICulture = ci;
        ApplicationLanguages.PrimaryLanguageOverride = "be-BY";
        Windows.ApplicationModel.Resources.Core.ResourceContext.GetForCurrentView().Reset();
        Windows.ApplicationModel.Resources.Core.ResourceContext.GetForViewIndependentUse().Reset();

        Window.Current.Activated += Current_Activated;

        //DataContext = Ioc.Default.GetRequiredService<ShellViewModel>();

        NavigationItems = new[]
        {
            new NavigationEntry(HomeItem, typeof(HomeView)),
            new NavigationEntry(PasswordsItem, typeof(PasswordsView)),
            new NavigationEntry(SettingsItem, typeof(SettingsView))
        };

        // Set the custom title bar to act as a draggable region
        Window.Current.SetTitleBar(TitleBarBorder);
        AppVersionTextBlock.Text = $"v{Assembly.GetAssembly(typeof(Shell)).GetName().Version.ToString()}";

        //WeakReferenceMessenger.Default.Register<InAppNotificationMessage>(this, HandleAppNotificationMessage);

    }

    private void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
    {
        _isWindowActive = e.WindowActivationState != CoreWindowActivationState.Deactivated;
    }

    // Select the introduction item when the shell is loaded
    private async void Shell_OnLoaded(object sender, RoutedEventArgs e)
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
