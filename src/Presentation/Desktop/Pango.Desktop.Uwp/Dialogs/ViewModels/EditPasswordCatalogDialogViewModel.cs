using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.NewPassword;
using Pango.Application.UseCases.Password.Commands.UpdatePassword;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
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
    private List<string> _availableCatalogs;
    private List<string> _existingCatalogs;
    private PasswordExplorerItem _selectedCatalog;

    public RelayCommand SaveCommand { get; }

    public EditPasswordCatalogDialogViewModel(ISender sender, ILogger<EditPasswordCatalogDialogViewModel> logger): base(logger)
    {
        _sender = sender;
        DialogContext = new DialogContext();
        SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
    }

    #region Properties

    public string NewCatalogName
    {
        get => _newCatalogName;
        set
        {
            SetProperty(ref _newCatalogName, value);
            DialogContext.RaiseDialogContentChanged(this);
        }
    }
    public IDialogContext DialogContext { get; }

    public string InitialCatalog
    {
        get => _initialCatalog;
        set
        {
            SetProperty(ref _initialCatalog, value);
            OnPropertyChanged(nameof(InitialCatalog));
        }
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

    

    #endregion

    #region Private Methods

    public bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(NewCatalogName) && !_existingCatalogs.Contains(NewCatalogName);
    }

    public void Initialize(EditCatalogParameters editCatalogParameters)
    {
        editCatalogParameters ??= new([], null, null, []);

        _selectedCatalog = editCatalogParameters.SelectedCatalog;
        _existingCatalogs = editCatalogParameters.ExistingCatalogs;

        AvailableCatalogs = editCatalogParameters.AllAvailableCatalogs ?? [];
        NewCatalogName = editCatalogParameters.SelectedCatalog?.Name ?? string.Empty;
        InitialCatalog = editCatalogParameters.SelectedCatalog?.CatalogPath ?? editCatalogParameters?.DefaultCatalog ?? string.Empty;

        IsNew = editCatalogParameters.SelectedCatalog is null;
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

            if (result.IsError)
            {
                Logger.LogError($"Creating catalog \"{NewCatalogName}\" failed: {result.FirstError}");
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(string.Format(ViewResourceLoader.GetString("CatalogCreationFailed_Format"), NewCatalogName), Core.Enums.AppNotificationType.Warning));
            }
            else
            {
                var entity = result.Value.Adapt<PangoPasswordListItemDto>();
                WeakReferenceMessenger.Default.Send(new PasswordCreatedMessage(entity));
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(string.Format(ViewResourceLoader.GetString("CatalogCreated_Format"), NewCatalogName)));
                Logger.LogDebug($"Catalog \"{NewCatalogName}\" successfully created");
            }
        }
        else
        {
            ErrorOr.ErrorOr<PangoPasswordDto> result = await _sender.Send(new UpdatePasswordCommand(_selectedCatalog.Id, NewCatalogName, null, null) { IsCatalogHolder = true, CatalogPath = InitialCatalog });

            if (result.IsError)
            {
                Logger.LogError($"Updating catalog \"{NewCatalogName}\" failed: {result.FirstError}");
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(string.Format(ViewResourceLoader.GetString("CatalogUpdateFailed_Format"), NewCatalogName), Core.Enums.AppNotificationType.Warning));
            }
            else
            {
                var entity = result.Value.Adapt<PangoPasswordListItemDto>();
                WeakReferenceMessenger.Default.Send(new PasswordUpdatedMessage(entity));
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(string.Format(ViewResourceLoader.GetString("CatalogUpdated_Format"), NewCatalogName)));

                Logger.LogDebug($"Catalog \"{NewCatalogName}\" successfully updated");
            }
        }
    }

    #endregion
}
