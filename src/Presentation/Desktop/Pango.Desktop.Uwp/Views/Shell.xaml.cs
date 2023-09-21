using CommunityToolkit.Mvvm.DependencyInjection;
using Pango.Desktop.Uwp.ViewModels;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Windows.Globalization;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Shell : UserControl
{
    #region Fields

    private bool _isWindowActive;

    #endregion

    internal ShellViewModel ViewModel => (ShellViewModel)DataContext;

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

    private void NavigateInitialPage()
    {
        SignInView signInView = new();
        SignInViewModel signInViewModel = signInView.DataContext as SignInViewModel;

        if (signInView != null)
        {
            signInViewModel.SignInSuceeded += SignInViewModel_SignInSuceeded;
        }

        AppContent.Content = signInView;
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

    private void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
    {
        _isWindowActive = e.WindowActivationState != CoreWindowActivationState.Deactivated;
    }

    // Select the introduction item when the shell is loaded
    private void Shell_OnLoaded(object sender, RoutedEventArgs e)
    {
        
    }
}
