using Microsoft.UI.Xaml.Controls;
using Pango.Application.Common.Interfaces.Services;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Views;
using System;
using System.Collections.Generic;

namespace Pango.Desktop.Uwp.Core.Navigation;

public class NavigationService : INavigationService
{
    private Frame _frame;

    // map AppView to Page type
    private readonly Dictionary<AppView, Type> _viewMap = new()
    {
        {AppView.Home, typeof(HomeView)},
        {AppView.PasswordsIndex, typeof(PasswordsView)},
        {AppView.User, typeof(UserView)},
        {AppView.ExportImport, typeof(ExportImportView)},
        {AppView.GeneratePassword, typeof(GeneratePasswordView)},
        {AppView.Settings, typeof(SettingsView)},
    };
    public void SetFrame(Frame frame)
    {
        _frame = frame;
    }
    public void Navigate(AppView view, object parameter = null)
    {
        if (_frame is null) return;
        if(_viewMap.TryGetValue(view, out var pageType))
        {
            _frame.Navigate(pageType, parameter);
        }
    }
    public bool CanGoBack => _frame?.CanGoBack ?? false;
    public void GoBack() => _frame?.GoBack();
}

