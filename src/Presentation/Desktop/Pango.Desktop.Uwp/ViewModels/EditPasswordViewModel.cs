using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Pango.Application.UseCases.Password.Commands.NewPassword;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.EditPassword)]
public class EditPasswordViewModel : ViewModelBase
{
    #region Fields

    private readonly ISender _sender;

    private string _name;
    private string _login;
    private string _value;

    #endregion

    public EditPasswordViewModel(ISender sender)
    {
        _sender = sender;

        OpenIndexViewCommand = new RelayCommand(OnOpenIndexView);
        SavePasswordComand = new RelayCommand(OnSavePassword);
    }

    #region Properties

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    #endregion

    #region Commands

    public RelayCommand OpenIndexViewCommand { get; }
    public RelayCommand SavePasswordComand { get; }

    #endregion

    #region Overrides

    public override Task OnNavigatedToAsync(object parameter)
    {
        Clear();

        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private void Clear()
    {
        Name = string.Empty; 
        Login = string.Empty; 
        Value = string.Empty;
    }

    private async void OnSavePassword()
    {
        await _sender.Send(new NewPasswordCommand(Name, Login, Value));
        OnOpenIndexView();
    }

    private void OnOpenIndexView()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.PasswordsIndex)));
    }

    #endregion
}
