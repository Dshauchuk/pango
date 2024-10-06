using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Views.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pango.Desktop.Uwp.Views;

public class ViewManager
{
    private static ViewManager? _default;
    public static ViewManager Default => _default ??= new ViewManager();

    private readonly Dictionary<AppView, ViewBase> _views;

    private readonly ILogger _logger;

    private ViewManager()
    {
        _views = [];
        _logger = App.Host.Services.GetRequiredService<ILogger<ViewManager>>();

        WeakReferenceMessenger.Default.Register<NavigationRequstedMessage>(this, OnViewNavigationRequested);
    }

    private void OnViewNavigationRequested(object recipient, NavigationRequstedMessage message)
    {
        if (message == null)
        {
            return;
        }

        ViewBase? sourceView = _views.ContainsKey(message.Value.SourceView) ? _views[message.Value.SourceView] : null;
        sourceView?.OnNavigatedFrom(message.Value);

        ViewBase? targetView = _views.ContainsKey(message.Value.NavigatedView) ? _views[message.Value.NavigatedView] : null;
        targetView?.OnNavigatedTo(message.Value);
    }

    public void Register(ViewBase view)
    {
        Type viewType = view.GetType();

        AppView? appView = ((viewType.GetCustomAttributes(true).FirstOrDefault(a => a.GetType() == typeof(AppViewAttribute)) as AppViewAttribute)?.View);
            
        if(appView == AppView.MainAppView || appView == AppView.SignIn)
        {
            _logger.LogDebug("{appView} view should not be registered", appView);
            return;
        }

        if(appView is null)
        {
            throw new InvalidCastException($"\"{viewType}\" view cannot be registered: {nameof(AppViewAttribute)} is missing");
        }
        else
        {
            if (!_views.TryAdd(appView.Value, view))
            {
                _views[appView.Value] = view;
            }

            _logger.LogDebug($"{appView} view has been registered");
        }
    }
}
