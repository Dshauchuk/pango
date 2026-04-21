using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.UseCases.Password.Queries.FindUserPassword;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Models.Parameters;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels;
using System;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public partial class PasswordDetailsDialogViewModel(ISender sender, ILogger<PasswordDetailsDialogViewModel> logger) : ViewModelBase(logger), IDialogViewModel
{
    #region Fields
    
    private readonly ISender _sender = sender;
    private PasswordDetailsParameters? _parameters;
    private string _name = string.Empty;
    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _catalog = string.Empty;
    private string _notes = string.Empty;
    private string _expirationDate = string.Empty;

    #endregion

    #region Properties

    public IDialogContext DialogContext { get; } = new DialogContext();

    public Guid PasswordId { get; private set; }

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

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string Catalog
    {
        get => _catalog;
        set => SetProperty(ref _catalog, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string ExpirationDate 
    { 
        get => _expirationDate; 
        set => SetProperty(ref _expirationDate, value); 
    }

    #endregion

    #region Overrides

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        if (parameter is not PasswordDetailsParameters dialogParameters)
        {
            return;
        }

        _parameters = dialogParameters;

        var passwordResult = await _sender.Send(new FindUserPasswordQuery(_parameters.PasswordId));

        if (passwordResult.IsError)
        {
            // log
            Logger.LogError("An error occurred while loading password details: {FirstError}", passwordResult.FirstError);
        }
        else
        {
            PasswordId = _parameters.PasswordId;
            Name = passwordResult.Value.Name;
            Login = passwordResult.Value.Login;
            Password = passwordResult.Value.Value;
            Catalog= passwordResult.Value.CatalogPath;

            Notes = passwordResult.Value.Properties.TryGetValue(
                PasswordProperties.Notes, out string? notes) ? notes : string.Empty;

            if (passwordResult.Value.Properties.TryGetValue(PasswordProperties.ExpirationDate, out string? expDateStr))
            {
                if (DateTimeOffset.TryParse(expDateStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var expDate) ||
                    DateTimeOffset.TryParse(expDateStr, System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.RoundtripKind, out expDate))
                {
                    ExpirationDate = expDate.LocalDateTime.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
                }
                else
                {
                    ExpirationDate = "-";
                }
            }
        }
    }

    #endregion

    #region Public Methods

    public bool CanSave()
    {
        return true;
    }

    public Task OnCancelAsync()
    {
        return Task.CompletedTask;
    }

    public Task OnSaveAsync()
    {
        WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.EditPassword, AppView.PasswordsIndex, new EditPasswordParameters(false, null, _parameters?.PasswordId, _parameters?.AllAvailableCatalogs))));

        return Task.CompletedTask;
    }

    #endregion
}
