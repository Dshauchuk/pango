using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pango.Application.Common.Interfaces.Services;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Views.Abstract;

namespace Pango.Desktop.Uwp.Views;

public class ViewManager
{
    private static ViewManager? _default;
    public static ViewManager Default => _default ??= new ViewManager();

    private readonly Dictionary<AppView, ViewBase> _views;

    private readonly ILogger _logger;
    private readonly INavigationService _navigationService;

    private ViewManager()
    {
        _views = [];
        _logger = App.Host.Services.GetRequiredService<ILogger<ViewManager>>();
        _navigationService = App.Host.Services.GetRequiredService<INavigationService>();

        WeakReferenceMessenger.Default.Register<NavigationRequstedMessage>(this, OnViewNavigationRequested);
    }

    private void OnViewNavigationRequested(object recipient, NavigationRequstedMessage message)
    {
        if (message == null)
        {
            return;
        }

        ViewBase? sourceView = _views.TryGetValue(message.Value.SourceView, out var sView) ? sView : null;
        sourceView?.OnNavigatedFrom(message.Value);

        ViewBase? targetView = _views.TryGetValue(message.Value.NavigatedView, out var tView) ? tView : null;
        targetView?.OnNavigatedTo(message.Value);

        _navigationService.Navigate(message.Value.NavigatedView, message.Value);
    }

    public void Register(ViewBase view)
    {
        Type viewType = view.GetType();

        AppView? appView = ((viewType.GetCustomAttributes(true).FirstOrDefault(a => a.GetType() == typeof(AppViewAttribute)) as AppViewAttribute)?.View);

        if (appView == AppView.MainAppView || appView == AppView.SignIn)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("{AppView} view should not be registered", appView.ToString());
            }
            return;
        }

        if (appView is null)
        {
            throw new InvalidCastException($"\"{viewType}\" view cannot be registered: {nameof(AppViewAttribute)} is missing");
        }
        else
        {
            if (!_views.TryAdd(appView.Value, view))
            {
                _views[appView.Value] = view;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("{AppView} view has been registered", appView.ToString());
            }
        }
    }
}
