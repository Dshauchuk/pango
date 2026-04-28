using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Pango.Application.Common.Interfaces.Services;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.Views;

namespace Pango.Desktop.Uwp.Core.Navigation;

public class NavigationService : INavigationService
{
    private Frame? _frame;
    private AppView? _currentView;
    private DateTime _lastNavTime = DateTime.MinValue;

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
        _frame.CacheSize = _viewMap.Count;
    }

    public void Navigate(AppView view, object? parameter = null)
    {
        if (_frame is null) return;

        if (!_frame.DispatcherQueue.HasThreadAccess)
        {
            _frame.DispatcherQueue.TryEnqueue(() => Navigate(view, parameter));
            return;
        }

        if ((DateTime.Now - _lastNavTime).TotalMilliseconds < 300)
            return;

        if (_currentView == view)
        {
            if (parameter is NavigationParameters navParams && navParams.Parameter == null)
                return;
            if (parameter == null)
                return;
        }

        if (_viewMap.TryGetValue(view, out var pageType))
        {
            _lastNavTime = DateTime.Now;
            _currentView = view;

            try
            {
                _frame.Navigate(pageType, parameter, new SuppressNavigationTransitionInfo());
                CleanBackStack(pageType);
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger?.Error(ex, "Navigation failed to {View}", view);
            }
        }
    }

    private void CleanBackStack(Type currentType)
    {
        if (_frame == null || _frame.BackStackDepth <= 1) return;

        var duplicates = _frame.BackStack.Where(b => b.SourcePageType == currentType).ToList();
        foreach (var duplicate in duplicates)
        {
            _frame.BackStack.Remove(duplicate);
        }
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
        {
            if (!_frame.DispatcherQueue.HasThreadAccess)
            {
                _frame.DispatcherQueue.TryEnqueue(GoBack);
                return;
            }

            _frame.GoBack(new SuppressNavigationTransitionInfo());

            var currentType = _frame.Content?.GetType();
            if (currentType != null)
            {
                var viewRecord = _viewMap.FirstOrDefault(x => x.Value == currentType);
                if (!viewRecord.Equals(default(KeyValuePair<AppView, Type>)))
                {
                    _currentView = viewRecord.Key;
                }
            }
        }
    }
}
