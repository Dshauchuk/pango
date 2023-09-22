using Pango.Desktop.Uwp.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Pango.Desktop.Uwp.Views.Abstract;

public abstract class ViewBase : UserControl
{
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
}
