using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pango.Application.Common.Interfaces.Services;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Views.Abstract;

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// Manages view registration and navigation within the application using a weak-reference dictionary.
/// </summary>
public class ViewManager
{
    /// <summary>
    /// Lazy-initialized singleton instance of ViewManager.
    /// </summary>
    private static ViewManager? _default;

    /// <summary>
    /// Gets the default singleton instance of ViewManager.
    /// </summary>
    public static ViewManager Default => _default ??= new ViewManager();

    /// <summary>
    /// Dictionary mapping AppView enums to weak references of ViewBase instances.
    /// </summary>
    private readonly Dictionary<AppView, WeakReference<ViewBase>> _views;

    /// <summary>
    /// Logger instance for diagnostic output.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Navigation service for handling view transitions.
    /// </summary>
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewManager"/> class and registers for navigation messages.
    /// </summary>
    private ViewManager()
    {
        _views = [];
        _logger = App.Host.Services.GetRequiredService<ILogger<ViewManager>>();
        _navigationService = App.Host.Services.GetRequiredService<INavigationService>();

        WeakReferenceMessenger.Default.Register<NavigationRequestedMessage>(this, OnViewNavigationRequested);
    }

    /// <summary>
    /// Handles navigation requests by invoking OnNavigatedFrom/OnNavigatedTo and triggering navigation.
    /// </summary>
    /// <param name="recipient">The message recipient (this ViewManager instance).</param>
    /// <param name="message">The navigation request message containing source and target view info.</param>
    private void OnViewNavigationRequested(object recipient, NavigationRequestedMessage message)
    {
        if (message == null)
        {
            return;
        }

        ViewBase? sourceView = _views.TryGetValue(message.Value.SourceView, out var sRef) && sRef.TryGetTarget(out var sView) ? sView : null;
        sourceView?.OnNavigatedFrom(message.Value);

        ViewBase? targetView = _views.TryGetValue(message.Value.NavigatedView, out var tRef) && tRef.TryGetTarget(out var tView) ? tView : null;
        targetView?.OnNavigatedTo(message.Value);

        _navigationService.Navigate(message.Value.NavigatedView, message.Value);
    }

    /// <summary>
    /// Registers a view instance if it has a valid AppViewAttribute and is not a reserved view type.
    /// </summary>
    /// <param name="view">The ViewBase instance to register.</param>
    public void Register(ViewBase view)
    {
        Type viewType = view.GetType();

        AppView? appView = (viewType.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType() == typeof(AppViewAttribute)) as AppViewAttribute)?.View;

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

        _views[appView.Value] = new WeakReference<ViewBase>(view);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("{AppView} view has been registered", appView.ToString());
        }
    }
}
