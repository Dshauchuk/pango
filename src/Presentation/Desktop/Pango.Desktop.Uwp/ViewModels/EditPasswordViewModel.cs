using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Pango.Application.Common;
using Pango.Application.UseCases.Password.Commands.NewPassword;
using Pango.Application.UseCases.Password.Queries.FindUserPassword;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels.Validators;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.EditPassword)]
public class EditPasswordViewModel : ViewModelBase
{
    #region Fields

    private readonly ISender _sender;
    private bool _isNew;

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

    public bool IsNew
    {
        get => _isNew;
        set => SetProperty(ref _isNew, value);
    }

    #endregion

    #region Commands

    public RelayCommand OpenIndexViewCommand { get; }
    public RelayCommand SavePasswordComand { get; }

    #endregion

    #region Overrides

    public override async Task OnNavigatedToAsync(object parameter)
    {
        Clear();

        if(parameter is null)
        {
            IsNew = true;
            return;
        }
        else
        {
            IsNew = false;
        }

        Guid passwordId = (Guid)parameter;
        var passwordResult = await _sender.Send(new FindUserPasswordQuery(passwordId));

        if (!passwordResult.IsError)
        {
            // DS
            // TODO: complete the implementation
        }
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
            Dictionary<string, string> props = new()
            {
                { PasswordProperties.Notes, PasswordValidator.Notes }
            };

            await _sender.Send(new NewPasswordCommand(PasswordValidator.Title, PasswordValidator.Login, PasswordValidator.Password, props));
            OnOpenIndexView();
        }
    }

    private void OnOpenIndexView()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.PasswordsIndex)));
    }

    #endregion
}
