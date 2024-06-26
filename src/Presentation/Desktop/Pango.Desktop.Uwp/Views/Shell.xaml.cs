using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Dialogs;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.Security;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using System;
using System.Linq;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[AppView(AppView.Shell)]
public sealed partial class Shell : ViewBase
{
    #region Fields

    private bool _isWindowActive;

    #endregion

    public Shell()
    {
        this.InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<ShellViewModel>();//App.Host.Services.GetRequiredService<ShellViewModel>();

        AppLanguageHelper.ApplyApplicationLanguage(AppLanguageHelper.GetAppliedAppLanguage() ?? AppLanguage.GetAppLanguageCollection().First());
        
        //App.Current.CurrentWindow.Activated += Current_Activated;
        
        //SetTitleBar();

        NavigateInitialPage();

        WeakReferenceMessenger.Default.Register<InAppNotificationMessage>(this, HandleAppNotificationMessage);
        WeakReferenceMessenger.Default.Register<NavigationRequstedMessage>(this, OnNavigationRequested);
        WeakReferenceMessenger.Default.Register<AppThemeChangedMessage>(this, OnAppThemeChanged);
    }

    private void OnAppThemeChanged(object recipient, AppThemeChangedMessage message)
    {
        ShellRootElement.RequestedTheme = message.Value;
    }

    private void OnNavigationRequested(object recipient, NavigationRequstedMessage message)
    {
        if(message.Value.NavigatedView == AppView.SignIn)
        {
            NavigateInitialPage();
        }
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

    private void HandleAppNotificationMessage(object recipient, InAppNotificationMessage message)
    {
        //object inAppNotificationWithButtonsTemplate;
        //bool isTemplatePresent = Resources.TryGetValue("InAppNotificationTemplate", out inAppNotificationWithButtonsTemplate);

        //if (isTemplatePresent && inAppNotificationWithButtonsTemplate is DataTemplate)
        //{
        //    InAppNotification.Show(inAppNotificationWithButtonsTemplate as DataTemplate,);
        //}

        InAppNotification.Show(message.Message, 3000);
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
        //App.Current.CurrentWindow.SetTitleBar(TitleBarBorder);
    }

    private void SignInViewModel_SignInSuceeded(string userId)
    {
        SetThreadPrincipal(userId);
        // Unsibscribe from the event to prevent memory leak
        SignInViewModel signInViewModel = (AppContent.Content as SignInView)?.DataContext as SignInViewModel;
        if (signInViewModel is not null)
        {
            signInViewModel.SignInSuceeded -= SignInViewModel_SignInSuceeded;
        }

        AppContent.Content = new MainAppView();
    }

    private void SetThreadPrincipal(string userId)
    {
        //IPrincipal principal = new GenericPrincipal(new GenericIdentity(userId, "Passport"), new string[] { });
        //IPrincipal principal = new GenericPrincipal(new GenericIdentity(userId, "Passport"), new string[] { });

        // Stores current user's principal
        //Thread.CurrentPrincipal = principal;
        // Stores application level principal, can be set only once
        // Uncomment if needed
        //AppDomain.CurrentDomain.SetThreadPrincipal(principal);

        SecureUserSession.SaveUser(userId);
    }

    private void Current_Activated(object sender, WindowActivatedEventArgs e)
    {
        _isWindowActive = e.WindowActivationState != WindowActivationState.Deactivated;
    }

    // Select the introduction item when the shell is loaded
    private void Shell_OnLoaded(object sender, RoutedEventArgs e)
    {

    }

    public async Task ShowContentDialogAsync(IContentDialog contentDialog)
    {
        ContentDialog dialog = new();

        // XamlRoot must be set in the case of a ContentDialog running in a Desktop app
        dialog.XamlRoot = this.XamlRoot;
        dialog.Style = Microsoft.UI.Xaml.Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        dialog.Title = "Save your work?";
        dialog.PrimaryButtonText = "Save";
        dialog.SecondaryButtonText = "Don't Save";
        dialog.CloseButtonText = "Cancel";
        dialog.DefaultButton = ContentDialogButton.Primary;
        dialog.Content = contentDialog;

        var result = await dialog.ShowAsync();

    }
}
