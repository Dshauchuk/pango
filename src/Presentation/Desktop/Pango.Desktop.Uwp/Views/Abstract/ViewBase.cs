using Microsoft.UI.Xaml.Controls;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.ViewModels;
using System.Diagnostics;

namespace Pango.Desktop.Uwp.Views.Abstract;

/// <summary>
/// Base class for UserControl views handled by a custom ViewManager.
/// </summary>
public abstract class ViewBase : UserControl
{
    public ViewBase()
    {
        ViewManager.Default.Register(this);
    }

    public IViewModel ViewModel => (IViewModel)DataContext;

    public virtual async void OnNavigatedTo(NavigationParameters? e)
    {
        Debug.WriteLine($"Navigated to {GetType().Name}");

        RegisterMessages();

        if (ViewModel != null)
        {
            await ViewModel.OnNavigatedToAsync(e);
        }
    }

    public virtual async void OnNavigatedFrom(NavigationParameters? e)
    {
        Debug.WriteLine($"Navigated from {GetType().Name}");

        UnregisterMessages();

        if (ViewModel != null)
        {
            await ViewModel.OnNavigatedFromAsync(e);
        }
    }

    protected virtual void RegisterMessages()
    {

    }

    protected virtual void UnregisterMessages()
    {

    }
}
