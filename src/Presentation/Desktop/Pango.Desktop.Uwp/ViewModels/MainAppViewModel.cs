using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

public sealed class MainAppViewModel : IViewModel
{
    public async Task OnNavigatedFromAsync(object parameter)
    {
        await Task.CompletedTask;
    }

    public async Task OnNavigatedToAsync(object parameter)
    {
        await Task.CompletedTask;
    }
}
