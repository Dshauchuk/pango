using CommunityToolkit.Mvvm.DependencyInjection;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using System;
using System.Globalization;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using Windows.Globalization;
using Windows.UI.Core;
using Windows.UI.Xaml;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Shell : ViewBase
{
    #region Fields

    private bool _isWindowActive;

    #endregion

    public Shell()
    {
        this.InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<ShellViewModel>();

        CultureInfo ci = new CultureInfo("be-BY");
        Thread.CurrentThread.CurrentCulture = ci;
        Thread.CurrentThread.CurrentUICulture = ci;
        ApplicationLanguages.PrimaryLanguageOverride = "be-BY";
        Windows.ApplicationModel.Resources.Core.ResourceContext.GetForCurrentView().Reset();
        Windows.ApplicationModel.Resources.Core.ResourceContext.GetForViewIndependentUse().Reset();

        Window.Current.Activated += Current_Activated;
        
        SetTitleBar();

        NavigateInitialPage();
    }

    private async void NavigateInitialPage()
    {
        SignInView signInView = new();
        SignInViewModel signInViewModel = signInView.DataContext as SignInViewModel;

        if (signInView != null)
        {
            signInViewModel.SignInSuceeded += SignInViewModel_SignInSuceeded;
        }

        AppContent.Content = signInView;
        await signInViewModel.OnNavigatedToAsync(null);
    }

    private void SetTitleBar()
    {
        // Set the custom title bar to act as a draggable region
        Window.Current.SetTitleBar(TitleBarBorder);
        AppVersionTextBlock.Text = $"v{Assembly.GetAssembly(typeof(Shell)).GetName().Version.ToString()}";
    }

    private void SignInViewModel_SignInSuceeded()
    {
        AppContent.Content = new MainAppView();
    }

    private void SetThreadPrincipal()
    {
        IPrincipal principal = new GenericPrincipal(new GenericIdentity("Bob", "Passport"), new string[] { "managers", "executives" });
        AppDomain.CurrentDomain.SetThreadPrincipal(principal);
    }

    private void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
    {
        _isWindowActive = e.WindowActivationState != CoreWindowActivationState.Deactivated;
    }

    // Select the introduction item when the shell is loaded
    private void Shell_OnLoaded(object sender, RoutedEventArgs e)
    {
        
    }
}
