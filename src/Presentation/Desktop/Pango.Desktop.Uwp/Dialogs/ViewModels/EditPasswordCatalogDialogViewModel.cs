using CommunityToolkit.Mvvm.Messaging;
using Mapster;
using MediatR;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.NewPassword;
using Pango.Application.UseCases.Password.Commands.UpdatePassword;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public class EditPasswordCatalogDialogViewModel : ViewModelBase, IDialogViewModel
{
    private readonly ISender _sender;
    private string _newCatalogName;
    private string _initialCatalog;
    private string _defaultCatalog;
    private List<string> _availableCatalogs;
    private PasswordExplorerItem _selectedCatalog;

    public EditPasswordCatalogDialogViewModel(ISender sender)
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
        set
        {
            SetProperty(ref _initialCatalog, value);
            OnPropertyChanged(nameof(InitialCatalog));
        }
    }

    public string DefaultCatalog
    {
        get => _defaultCatalog;
        set => SetProperty(ref _defaultCatalog, value);
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

    public bool IsNew { get; private set; }

    public bool HasCatalogs => AvailableCatalogs != null && AvailableCatalogs.Any();

    public void Initialize(List<string>? catalogs = null, string? defaultCatalog = null, PasswordExplorerItem? catalog = null)
    {
        _selectedCatalog = catalog;

        AvailableCatalogs = catalogs ?? [];
        NewCatalogName = catalog?.Name ?? string.Empty;
        DefaultCatalog = string.Empty;
        InitialCatalog = catalog?.CatalogPath ?? defaultCatalog ?? string.Empty;

        IsNew = catalog is null;
    }

    public Task OnCancelAsync()
    {
        return Task.CompletedTask;
    }

    public async Task OnSaveAsync()
    {
        if (IsNew)
        {
            ErrorOr.ErrorOr<PangoPasswordDto> result = await _sender.Send(new NewPasswordCommand(NewCatalogName, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()) { IsCatalogHolder = true, CatalogPath = InitialCatalog });
            if (!result.IsError)
            {
                var entity = result.Value.Adapt<PangoPasswordListItemDto>();
                WeakReferenceMessenger.Default.Send(new PasswordCreatedMessage(entity));
            }
        }
        else
        {
            ErrorOr.ErrorOr<PangoPasswordDto> result = await _sender.Send(new UpdatePasswordCommand(_selectedCatalog.Id, NewCatalogName, null, null) { IsCatalogHolder = true, CatalogPath = InitialCatalog });
            if (!result.IsError)
            {
                var entity = result.Value.Adapt<PangoPasswordListItemDto>();
                WeakReferenceMessenger.Default.Send(new PasswordUpdatedMessage(entity));
            }
        }
    }
}
