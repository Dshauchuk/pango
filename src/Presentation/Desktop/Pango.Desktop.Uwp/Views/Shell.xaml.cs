using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Core;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Desktop.Uwp.Views.Abstract;
using Serilog;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// Main shell page that serves as the application container and handles navigation, theming, and messaging.
/// </summary>
[AppView(AppView.Shell)]
public sealed partial class Shell : ViewBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Shell"/> class.
    /// </summary>
    public Shell()
    {
        InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<ShellViewModel>();

        SetApplicationLanguage();
        NavigateInitialPageAsync();
        RegisterMessages();
    }

    #region Overrides

    /// <summary>
    /// Registers message subscriptions for notifications, navigation, theme, and language changes.
    /// </summary>
    protected override void RegisterMessages()
    {
        base.RegisterMessages();

        WeakReferenceMessenger.Default.Register<InAppNotificationMessage>(this, HandleAppNotificationMessage);
        WeakReferenceMessenger.Default.Register<NavigationRequestedMessage>(this, OnNavigationRequested);
        WeakReferenceMessenger.Default.Register<AppThemeChangedMessage>(this, OnAppThemeChanged);
        WeakReferenceMessenger.Default.Register<AppLanguageChangedMessage>(this, OnAppLanguageChanged);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles application theme change messages and updates UI colors accordingly.
    /// </summary>
    /// <param name="recipient">The message recipient instance.</param>
    /// <param name="message">The theme change message containing the new theme value.</param>
    private void OnAppThemeChanged(object recipient, AppThemeChangedMessage message)
    {
        ShellRootElement.RequestedTheme = message.Value;

        var buttonColor = message.Value == ElementTheme.Dark ? Colors.Black : Colors.White;
        TitleBarHelper.SetCaptionButtonColors(App.Current.CurrentWindow, buttonColor);
    }

    /// <summary>
    /// Handles navigation requests and redirects to initial page if sign-in is requested.
    /// </summary>
    /// <param name="recipient">The message recipient instance.</param>
    /// <param name="message">The navigation request message.</param>
    private void OnNavigationRequested(object recipient, NavigationRequestedMessage message)
    {
        if (message.Value.NavigatedView == AppView.SignIn)
        {
            NavigateInitialPageAsync();
        }
    }

    /// <summary>
    /// Handles application language change messages and updates the content view.
    /// </summary>
    /// <param name="recipient">The message recipient instance.</param>
    /// <param name="message">The language change message containing the new language.</param>
    private void OnAppLanguageChanged(object recipient, AppLanguageChangedMessage message)
    {
        if (message.Value is null)
            return;

        AppContent.Content = new MainAppView(message.Value);
    }

    /// <summary>
    /// Handles in-app notification messages and displays them via the notification control.
    /// </summary>
    /// <param name="recipient">The message recipient instance.</param>
    /// <param name="message">The notification message containing content and severity.</param>
    private void HandleAppNotificationMessage(object recipient, InAppNotificationMessage message)
    {
        App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            var notification = new Notification
            {
                Message = message.Message,
                Severity = CastSeverity(message.Type),
                IsIconVisible = true,
                Duration = TimeSpan.FromMilliseconds(Constants.InAppNotificationDuration)
            };
            InAppNotification.Show(notification);
        });
    }

    /// <summary>
    /// Converts application notification type to InfoBar severity enum value.
    /// </summary>
    /// <param name="notificationType">The source notification type.</param>
    /// <returns>Corresponding <see cref="InfoBarSeverity"/> value.</returns>
    /// <exception cref="InvalidCastException">Thrown when an unknown notification type is provided.</exception>
    private InfoBarSeverity CastSeverity(AppNotificationType notificationType)
    {
        return notificationType switch
        {
            AppNotificationType.Info => InfoBarSeverity.Informational,
            AppNotificationType.Warning => InfoBarSeverity.Warning,
            AppNotificationType.Error => InfoBarSeverity.Error,
            AppNotificationType.Success => InfoBarSeverity.Success,
            _ => throw new InvalidCastException($"Unknown value of {nameof(AppNotificationType)}: {notificationType}")
        };
    }

    /// <summary>
    /// Handles successful sign-in event: unsubscribes and navigates to main application view.
    /// </summary>
    /// <param name="userId">The authenticated user identifier.</param>
    private void SignInViewModel_SignInSucceeded(string userId)
    {
        Log.Logger?.Information("User signed in successfully: {UserId}", userId);

        App.Current.LoginSucceeded -= SignInViewModel_SignInSucceeded;
        AppContent.Content = new MainAppView();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Applies the currently configured application language on startup.
    /// </summary>
    private static void SetApplicationLanguage()
    {
        var language = AppLanguageHelper.GetAppliedAppLanguage() ?? AppLanguage.GetAppLanguageCollection().First();
        AppLanguageHelper.ApplyApplicationLanguage(language);
    }

    /// <summary>
    /// Navigates to the initial sign-in page asynchronously and wires up login success handler.
    /// </summary>
    private async void NavigateInitialPageAsync()
    {
        var signInView = new SignInView();

        if (signInView.DataContext is SignInViewModel signInViewModel)
        {
            App.Current.LoginSucceeded += SignInViewModel_SignInSucceeded;

            AppContent.Content = signInView;
            await signInViewModel.OnNavigatedToAsync(null);
        }
    }

    /// <summary>
    /// Handles the Loaded event of the Shell control; currently reserved for future initialization logic.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="routedEventArgs">Event data.</param>
    private void Shell_OnLoaded(object sender, RoutedEventArgs routedEventArgs)
    {
        Log.Logger?.Debug("Shell view loaded");
    }

    #endregion
}
