using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

public sealed class HomeViewModel : ObservableRecipient, IViewModel
{
    public HomeViewModel()
    {
    }

    public async Task OnNavigatedFromAsync(object parameter)
    {
        await Task.CompletedTask;
    }

    public async Task OnNavigatedToAsync(object parameter)
    {
        await Task.CompletedTask;
    }
}
