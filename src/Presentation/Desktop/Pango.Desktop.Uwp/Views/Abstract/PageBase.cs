using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Pango.Desktop.Uwp.ViewModels;
using System.Diagnostics;

namespace Pango.Desktop.Uwp.Views.Abstract;

public abstract class PageBase(ILogger logger) : Page
{
    public IViewModel ViewModel => (IViewModel)DataContext;

    protected ILogger Logger { get; } = logger;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        Debug.WriteLine($"Navigated to {this.GetType().Name}");

        RegisterMessages();

        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Debug.WriteLine($"Navigated from {this.GetType().Name}");

        UnregisterMessages();

        base.OnNavigatedFrom(e);
    }

    protected virtual void RegisterMessages()
    {

    }

    protected virtual void UnregisterMessages()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
