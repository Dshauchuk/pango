using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Pango.Desktop.Uwp.Mvvm.Messages;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

public class EditPasswordViewModel : ObservableObject, IViewModel
{
    private string _text;

    public EditPasswordViewModel()
    {
        Text = "Hello world";

        OpenIndexViewCommand = new RelayCommand(OnOpenIndexView);
    }

    private void OnOpenIndexView()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(Core.Enums.AppView.PasswordsIndex));
    }

    public RelayCommand OpenIndexViewCommand { get; }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
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
