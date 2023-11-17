using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
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

        AppLanguageHelper.ApplyApplicationLanguage(AppLanguageHelper.GetAppliedAppLanguage() ?? AppLanguage.GetAppLanguageCollection().First());
        
        Window.Current.Activated += Current_Activated;
        
        SetTitleBar();

        NavigateInitialPage();
    }

    #region Overrides

    protected override void RegisterMessages()
    {
        base.RegisterMessages();

        WeakReferenceMessenger.Default.Register<AppLanguageChangedMessage>(this, OnAppLanguageChanged);
    }

    private void OnAppLanguageChanged(object recipient, AppLanguageChangedMessage message)
    {
        if (message.Value is null)
            return;

        AppContent.Content = new MainAppView(message.Value);
    }

    #endregion

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

    private void SignInViewModel_SignInSuceeded(string userId)
    {
        SetThreadPrincipal(userId);
        AppContent.Content = new MainAppView();
    }

    private void SetThreadPrincipal(string userId)
    {
        IPrincipal principal = new GenericPrincipal(new GenericIdentity(userId.ToLower(), "Passport"), new string[] { });

        Thread.CurrentPrincipal = principal;
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
