using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.Security;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using System.Linq;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[AppView(AppView.Shell)]
public sealed partial class Shell : ViewBase
{
    public Shell()
    {
        this.InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<ShellViewModel>();

        SetApplicationLanguage();
        NavigateInitialPage();
        RegisterMessages();
    }

    #region Overrides

    protected override void RegisterMessages()
    {
        base.RegisterMessages();

        WeakReferenceMessenger.Default.Register<InAppNotificationMessage>(this, HandleAppNotificationMessage);
        WeakReferenceMessenger.Default.Register<NavigationRequstedMessage>(this, OnNavigationRequested);
        WeakReferenceMessenger.Default.Register<AppThemeChangedMessage>(this, OnAppThemeChanged);
        WeakReferenceMessenger.Default.Register<AppLanguageChangedMessage>(this, OnAppLanguageChanged);
    }

    #endregion

    #region Event Handlers

    private void OnAppThemeChanged(object recipient, AppThemeChangedMessage message)
    {
        ShellRootElement.RequestedTheme = message.Value;
    }

    private void OnNavigationRequested(object recipient, NavigationRequstedMessage message)
    {
        if (message.Value.NavigatedView == AppView.SignIn)
        {
            NavigateInitialPage();
        }
    }

    private void OnAppLanguageChanged(object recipient, AppLanguageChangedMessage message)
    {
        if (message.Value is null)
            return;

        AppContent.Content = new MainAppView(message.Value);
    }


    private void HandleAppNotificationMessage(object recipient, InAppNotificationMessage message)
    {
        InAppNotification.Show(message.Message, 3000);
    }

    private void SignInViewModel_SignInSuceeded(string userId)
    {
        SetupSession(userId);

        // Unsibscribe from the event to prevent memory leak
        SignInViewModel? signInViewModel = (AppContent.Content as SignInView)?.DataContext as SignInViewModel;
        if (signInViewModel is not null)
        {
            signInViewModel.SignInSuceeded -= SignInViewModel_SignInSuceeded;
        }

        AppContent.Content = new MainAppView();
    }

    #endregion

    #region Private Methods

    private void SetApplicationLanguage()
    {
        AppLanguageHelper.ApplyApplicationLanguage(AppLanguageHelper.GetAppliedAppLanguage() ?? AppLanguage.GetAppLanguageCollection().First());
    }

    private async void NavigateInitialPage()
    {
        SignInView signInView = new();

        if (signInView.DataContext is SignInViewModel signInViewModel)
        {
            signInViewModel.SignInSuceeded += SignInViewModel_SignInSuceeded;

            AppContent.Content = signInView;
            await signInViewModel.OnNavigatedToAsync(null);
        }
    }

    private static void SetupSession(string userId)
    {
        SecureUserSession.SaveUser(userId);
    }

    // Select the introduction item when the shell is loaded
    private void Shell_OnLoaded(object sender, RoutedEventArgs e)
    {

    }

    #endregion
}
