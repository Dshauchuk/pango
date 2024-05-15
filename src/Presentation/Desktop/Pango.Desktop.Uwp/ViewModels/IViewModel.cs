using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

public interface IViewModel
{
    Task OnNavigatedToAsync(object parameter);

    Task OnNavigatedFromAsync(object parameter);
}
