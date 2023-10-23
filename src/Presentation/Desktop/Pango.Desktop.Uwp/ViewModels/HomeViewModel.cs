using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

public sealed class HomeViewModel : ObservableRecipient, IViewModel
{
    public HomeViewModel()
    {
    }

    public Task OnNavigatedToAsync(object parameter)
    {
        return Task.CompletedTask;
    }
}
