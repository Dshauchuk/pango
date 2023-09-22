using Pango.Desktop.Uwp.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Pango.Desktop.Uwp.Views.Abstract;

public abstract class PageBase : Page
{
    public IViewModel ViewModel => (IViewModel)DataContext;

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (ViewModel != null)
            await ViewModel.OnNavigatedToAsync(e.Parameter);
    }

    protected override async void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (ViewModel != null)
            await ViewModel.OnNavigatedFromAsync(e.Parameter);
    }
}
