using CommunityToolkit.Mvvm.Messaging;
using Mapster;
using MediatR;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.NewPassword;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public class NewPasswordCatalogDialogViewModel : ViewModelBase, IDialogViewModel
{
    private readonly ISender _sender;
    private string _newCatalogName;
    private string _initialCatalog;
    private List<string> _availableCatalogs;

    public NewPasswordCatalogDialogViewModel(ISender sender)
    {
        _sender = sender;
    }

    public string NewCatalogName
    {
        get => _newCatalogName;
        set => SetProperty(ref _newCatalogName, value);
    }

    public string InitialCatalog
    {
        get => _initialCatalog;
        set => SetProperty(ref _initialCatalog, value);
    }

    public List<string> AvailableCatalogs
    {
        get => _availableCatalogs;
        set => SetProperty(ref _availableCatalogs, value);
    }

    public Task OnCancelAsync()
    {
        return Task.CompletedTask;
    }

    public async Task OnSaveAsync()
    {
        ErrorOr.ErrorOr<PangoPasswordDto> result = await _sender.Send(new NewPasswordCommand(NewCatalogName, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()) { IsCatalogHolder = true, CatalogPath = InitialCatalog });

        if(!result.IsError)
        {
            var entity = result.Value.Adapt<PangoPasswordListItemDto>();

            WeakReferenceMessenger.Default.Send(new PasswordCreatedMessage(entity));
        }
    }
}
