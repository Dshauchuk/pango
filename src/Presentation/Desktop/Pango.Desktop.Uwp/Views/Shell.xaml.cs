﻿using CommunityToolkit.Mvvm.Messaging;
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
using System;
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

        if(message.Value == ElementTheme.Dark)
        {
            TitleBarHelper.SetCaptionButtonColors(App.Current.CurrentWindow, Colors.Black);
        }
        else
        {
            TitleBarHelper.SetCaptionButtonColors(App.Current.CurrentWindow, Colors.White);
        }

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
        var notification = new Notification()
        {
            Message = message.Message,
            Severity = CastSeverity(message.Type),
            IsIconVisible = true,
            Duration = TimeSpan.FromMilliseconds(Constants.InAppNotificationDuration)
        };

        InAppNotification.Show(notification);
    }

    InfoBarSeverity CastSeverity(AppNotificationType notificationType)
    {
        return notificationType switch
        {
            AppNotificationType.Info => InfoBarSeverity.Informational,
            AppNotificationType.Warning => InfoBarSeverity.Warning,
            AppNotificationType.Error => InfoBarSeverity.Error,
            AppNotificationType.Success => InfoBarSeverity.Success,
            _ => throw new InvalidCastException($"Unkown value of {nameof(AppNotificationType)}: {notificationType}")
        };
    }

    private void SignInViewModel_SignInSuceeded(string userId)
    {
        App.Current.LoginSucceeded -= SignInViewModel_SignInSuceeded;

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
            App.Current.LoginSucceeded += SignInViewModel_SignInSuceeded;

            AppContent.Content = signInView;
            await signInViewModel.OnNavigatedToAsync(null);
        }
    }

    // Select the introduction item when the shell is loaded
    private void Shell_OnLoaded(object sender, RoutedEventArgs e)
    {

    }

    #endregion
}
