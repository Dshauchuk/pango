using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.EditPassword)]
public class EditPasswordViewModel : ViewModelBase
{
    private string _text;

    public EditPasswordViewModel()
    {
        Text = "Hello world";

        OpenIndexViewCommand = new RelayCommand(OnOpenIndexView);
    }

    private void OnOpenIndexView()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.PasswordsIndex)));
    }

    public RelayCommand OpenIndexViewCommand { get; }
    public RelayCommand SavePasswordComand { get; }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }
}
