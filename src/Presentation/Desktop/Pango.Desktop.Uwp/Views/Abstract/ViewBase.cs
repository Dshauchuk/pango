using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Pango.Desktop.Uwp.ViewModels;

namespace Pango.Desktop.Uwp.Views.Abstract;

public abstract class ViewBase : UserControl
{
    public ViewBase()
    {
        RegisterMessages();
    }

    public IViewModel ViewModel => (IViewModel)DataContext;

    protected virtual async void OnNavigatedTo(NavigationEventArgs e)
    {
        if (ViewModel != null)
            await ViewModel.OnNavigatedToAsync(e.Parameter);
    }

    protected virtual async void OnNavigatedFrom(NavigationEventArgs e)
    {
        if (ViewModel != null)
            await ViewModel.OnNavigatedFromAsync(e.Parameter);
    }

    protected virtual void RegisterMessages()
    {

    }
}
