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

public class PasswordDetailsDialogViewModel : ViewModelBase, IDialogViewModel
{
    private readonly ISender _sender;
    private PasswordDetailsParameters? _parameters;

    public PasswordDetailsDialogViewModel(ISender sender, ILogger<PasswordDetailsDialogViewModel> logger) : base(logger)
    {
        DialogContext = new DialogContext();
        _sender = sender;
    }

    public IDialogContext DialogContext { get; }

    public Guid PasswordId { get; private set; }

    private string _name = string.Empty;
    public string Name
    { 
        get => _name;
        set => SetProperty(ref _name, value); 
    }

    private string _login = string.Empty;
    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    private string _catalog = string.Empty;
    public string Catalog
    {
        get => _catalog;
        set => SetProperty(ref _catalog, value);
    }

    private string _notes = string.Empty;
    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

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

            if (passwordResult.Value.Properties.TryGetValue(PasswordProperties.Notes, out string? value))
            {
                Notes = value;
            }
        }
    }

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
}
