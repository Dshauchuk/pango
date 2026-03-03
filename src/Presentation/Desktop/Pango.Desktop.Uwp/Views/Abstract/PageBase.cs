using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Pango.Desktop.Uwp.ViewModels;
using System.Diagnostics;

namespace Pango.Desktop.Uwp.Views.Abstract;

/// <summary>
/// Base class for pages. Uses NavigationCacheMode.Required to prevent UI recreation and drastically reduce memory spikes when switching tabs.
/// </summary>
public abstract class PageBase : Page
{
    public IViewModel ViewModel => (IViewModel)DataContext;
    protected ILogger Logger { get; }

    public PageBase(ILogger logger)
    {
        Logger = logger;

        NavigationCacheMode = NavigationCacheMode.Required;
    }

    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
        Debug.WriteLine($"Navigated to {GetType().Name}");
        RegisterMessages();

        if (DataContext is ViewModelBase viewModel)
        {
            await viewModel.OnNavigatedToAsync(e.Parameter);
        }

        base.OnNavigatedTo(e);
    }

    protected async override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Debug.WriteLine($"Navigated from {GetType().Name}");
        UnregisterMessages();

        if (DataContext is ViewModelBase viewModel)
        {
            await viewModel.OnNavigatedFromAsync(e.Parameter);
        }

        base.OnNavigatedFrom(e);
    }

    protected virtual void RegisterMessages() { }
    protected virtual void UnregisterMessages() { WeakReferenceMessenger.Default.UnregisterAll(this); }
}
