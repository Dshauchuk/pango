using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Pango.Application.UseCases.Password.Commands.NewPassword;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels.Validators;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.EditPassword)]
public class EditPasswordViewModel : ViewModelBase
{
    #region Fields

    private readonly ISender _sender;

    private EditPasswordValidator _passwordValidator;

    #endregion

    public EditPasswordViewModel(ISender sender)
    {
        _sender = sender;

        OpenIndexViewCommand = new RelayCommand(OnOpenIndexView);
        SavePasswordComand = new RelayCommand(OnSavePassword);
    }

    #region Properties

    public EditPasswordValidator PasswordValidator
    {
        get => _passwordValidator;
        set => SetProperty(ref _passwordValidator, value);
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
        PasswordValidator = new EditPasswordValidator();
    }

    private async void OnSavePassword()
    {
        PasswordValidator.Validate();

        if (!PasswordValidator.HasErrors)
        {
            await _sender.Send(new NewPasswordCommand(PasswordValidator.Title, PasswordValidator.Login, PasswordValidator.Password));
            OnOpenIndexView();
        }
    }

    private void OnOpenIndexView()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.PasswordsIndex)));
    }

    #endregion
}
