using CommunityToolkit.Mvvm.ComponentModel;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.Home)]
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
