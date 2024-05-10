﻿using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Pango.Application.Common;
using Pango.Application.UseCases.Password.Commands.NewPassword;
using Pango.Application.UseCases.Password.Commands.UpdatePassword;
using Pango.Application.UseCases.Password.Queries.FindUserPassword;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Models.Parameters;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.ViewModels.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.EditPassword)]
public class EditPasswordViewModel : ViewModelBase
{
    #region Fields

    private readonly ISender _sender;
    private bool _isNew;
    private List<string> _availableCatalogs;

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

    public List<string> AvailableCatalogs
    {
        get => _availableCatalogs;
        set
        {
            SetProperty(ref _availableCatalogs, value);
            OnPropertyChanged(nameof(HasCatalogs));
        }
    }

    public bool HasCatalogs => AvailableCatalogs != null && AvailableCatalogs.Any();

    #endregion

    #region Commands

    public RelayCommand OpenIndexViewCommand { get; }
    public RelayCommand SavePasswordComand { get; }

    #endregion

    #region Overrides

    public override async Task OnNavigatedToAsync(object parameter)
    {
        Clear();

        NavigationParameters navigationParameters = parameter as NavigationParameters;

        if (navigationParameters.Value is not EditPasswordParameters parameters)
        {
            IsNew = true;
            PasswordValidator.SelectedCatalog = null;

            return;
        }
        else
        {
            IsNew = parameters.IsNew;
            AvailableCatalogs = parameters.AvailableCatalogs;
            PasswordValidator.SelectedCatalog = parameters.Catalog;

            if (!IsNew && parameters.SelectedPasswordId != null)
            {
                Guid passwordId = parameters.SelectedPasswordId.Value;
                var passwordResult = await _sender.Send(new FindUserPasswordQuery(passwordId));

                if (!passwordResult.IsError)
                {
                    PasswordValidator.Id = passwordId;
                    PasswordValidator.Login = passwordResult.Value.Login;
                    PasswordValidator.Title = passwordResult.Value.Name;
                    PasswordValidator.Password = passwordResult.Value.Value;

                    if (passwordResult.Value.Properties.ContainsKey(PasswordProperties.Notes))
                    {
                        PasswordValidator.Notes = passwordResult.Value.Properties[PasswordProperties.Notes];
                    }
                }
            }
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

            if (IsNew)
            {
                await _sender.Send(new NewPasswordCommand(PasswordValidator.Title, PasswordValidator.Login, PasswordValidator.Password, props) { CatalogPath = PasswordValidator.SelectedCatalog });
            }
            else
            {
                await _sender.Send(new UpdatePasswordCommand(PasswordValidator.Id.Value, PasswordValidator.Title, PasswordValidator.Login, PasswordValidator.Password, props));
            }
            
            OnOpenIndexView();

            string message = IsNew ? string.Format(ViewResourceLoader.GetString("PasswordCreated"), PasswordValidator.Title) : string.Format(ViewResourceLoader.GetString("PasswordModified"), PasswordValidator.Title);
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(message));
        }
    }

    private void OnOpenIndexView()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.PasswordsIndex)));
    }

    #endregion
}
