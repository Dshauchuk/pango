using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Models;
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
    private List<string>? _availableCatalogs;
    private EditPasswordValidator? _passwordValidator;

    #endregion

    public EditPasswordViewModel(ISender sender, ILogger<EditPasswordViewModel> logger): base(logger)
    {
        _sender = sender;

        OpenIndexViewCommand = new RelayCommand(OnOpenIndexView);
        SavePasswordComand = new RelayCommand(OnSavePassword);
    }

    #region Properties

    public EditPasswordValidator? PasswordValidator
    {
        get => _passwordValidator;
        set => SetProperty(ref _passwordValidator, value);
    }

    public bool IsNew
    {
        get => _isNew;
        set => SetProperty(ref _isNew, value);
    }

    public List<string>? AvailableCatalogs
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

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        Clear();

        NavigationParameters? navigationParameters = parameter as NavigationParameters;

        if (navigationParameters?.Parameter is not EditPasswordParameters parameters)
        {
            IsNew = true;
            PasswordValidator!.SelectedCatalog = null;

            return;
        }
        else
        {
            IsNew = parameters.IsNew;
            AvailableCatalogs = parameters.AvailableCatalogs;

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
                    PasswordValidator.SelectedCatalog = passwordResult.Value.CatalogPath;

                    if (passwordResult.Value.Properties.ContainsKey(PasswordProperties.Notes))
                    {
                        PasswordValidator.Notes = passwordResult.Value.Properties[PasswordProperties.Notes];
                    }
                }
            }
            else
            {
                PasswordValidator!.SelectedCatalog = parameters.Catalog;
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
        PasswordValidator!.Validate();

        if (!PasswordValidator.HasErrors)
        {
            ErrorOr.ErrorOr<PangoPasswordDto> result;

            if (IsNew)
            {
                result = 
                    await _sender.Send(
                        new NewPasswordCommand(
                            PasswordValidator.Title, 
                            PasswordValidator.Login, 
                            PasswordValidator.Password, 
                            new Dictionary<string, string>() { { PasswordProperties.Notes, PasswordValidator.Notes }}) 
                        { 
                            CatalogPath = PasswordValidator.SelectedCatalog ?? string.Empty
                        });

                if(result.IsError)
                {
                    Logger.LogError("Creating password \"{Title}\" failed: {FirstError}", PasswordValidator.Title, result.FirstError);
                }
                else
                {
                    Logger.LogDebug($"Password \"{PasswordValidator.Title}\" successfully created");
                    WeakReferenceMessenger.Default.Send(new PasswordCreatedMessage(result.Value.Adapt<PangoPasswordListItemDto>()));
                }
            }
            else
            {
                result = 
                    await _sender.Send(
                        new UpdatePasswordCommand(
                            PasswordValidator.Id!.Value, 
                            PasswordValidator.Title, 
                            PasswordValidator.Login, 
                            PasswordValidator.Password, 
                            new Dictionary<string, string>() { { PasswordProperties.Notes, PasswordValidator.Notes } }) 
                        { 
                            CatalogPath = PasswordValidator.SelectedCatalog ?? string.Empty
                        });

                if (result.IsError)
                {
                    Logger.LogError("Updating password \"{Title}\" failed: {FirstError}", PasswordValidator.Title, result.FirstError);
                }
                else
                {
                    Logger.LogDebug("Password \"{Title}\" successfully updated", PasswordValidator.Title);
                    WeakReferenceMessenger.Default.Send(new PasswordUpdatedMessage(result.Value.Adapt<PangoPasswordListItemDto>()));
                }
            }
            
            OnOpenIndexView();

            string message = IsNew ? string.Format(ViewResourceLoader.GetString("PasswordCreated"), PasswordValidator.Title) : string.Format(ViewResourceLoader.GetString("PasswordModified"), PasswordValidator.Title);
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(message));
        }
    }

    private void OnOpenIndexView()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.PasswordsIndex, AppView.EditPassword)));
    }

    #endregion
}
