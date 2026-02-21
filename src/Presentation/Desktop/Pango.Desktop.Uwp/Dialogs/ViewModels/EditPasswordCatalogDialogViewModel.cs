using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.NewPassword;
using Pango.Application.UseCases.Password.Commands.UpdatePassword;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public partial class EditPasswordCatalogDialogViewModel : ViewModelBase, IDialogViewModel, INotifyDataErrorInfo
{
    #region Fields

    private readonly ISender _sender;
    private string _newCatalogName = string.Empty;
    private string _initialCatalog = string.Empty;
    private List<string>? _availableCatalogs;
    private List<string>? _existingCatalogs;
    private PangoExplorerItem? _selectedCatalog;
    private readonly Dictionary<string, List<string>> _errors = [];

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="EditPasswordCatalogDialogViewModel"/> with dependencies.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands.</param>
    /// <param name="logger">Logger for tracing operations and errors.</param>
    public EditPasswordCatalogDialogViewModel(ISender sender, ILogger<EditPasswordCatalogDialogViewModel> logger) : base(logger)
    {
        _sender = sender;
        DialogContext = new DialogContext();
        SaveCommand = new RelayCommand(async () => await OnSaveAsync(), CanSave);
    }

    #region Commands

    public RelayCommand SaveCommand { get; }

    #endregion

    #region Properties

    public string NewCatalogName
    {
        get => _newCatalogName;
        set
        {
            if (SetProperty(ref _newCatalogName, value))
            {
                ValidateName();
                DialogContext.RaiseDialogContentChanged(this);
                SaveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string NameError
    {
        get
        {
            var errors = GetErrors(nameof(NewCatalogName));
            return errors.Cast<string>().FirstOrDefault() ?? string.Empty;
        }
    }

    public IDialogContext DialogContext { get; }

    public string InitialCatalog
    {
        get => _initialCatalog;
        set
        {
            if (SetProperty(ref _initialCatalog, value))
            {
                OnPropertyChanged(nameof(InitialCatalog));
                ValidateName();
                DialogContext.RaiseDialogContentChanged(this);
            }
        }
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

    public bool IsNew { get; private set; }

    public bool HasCatalogs => AvailableCatalogs != null && AvailableCatalogs.Count != 0;

    #endregion

    #region Validation (INotifyDataErrorInfo)

    public bool HasErrors => _errors.Count > 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>
    /// Returns all validation errors for the specified property name, or empty collection if none exist.
    /// </summary>
    /// <param name="propertyName">The name of the property to retrieve errors for.</param>
    /// <returns>An enumerable of error messages.</returns>
    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out var errors))
        {
            return Enumerable.Empty<object>();
        }
        return errors;
    }

    /// <summary>
    /// Validates the catalog name for invalid characters and uniqueness against available catalogs.
    /// </summary>
    private void ValidateName()
    {
        string propertyName = nameof(NewCatalogName);
        ClearErrors(propertyName);

        if (string.IsNullOrWhiteSpace(NewCatalogName))
        {
            OnPropertyChanged(nameof(NameError));
            return;
        }

        char[] invalidFileSystemChars = Path.GetInvalidFileNameChars();
        if (NewCatalogName.Contains(AppConstants.CatalogDelimeter) || NewCatalogName.IndexOfAny(invalidFileSystemChars) >= 0)
        {
            AddError(propertyName, ViewResourceLoader.GetString("ValidationError_InvalidChars"));
        }

        string separator = AppConstants.CatalogDelimeter.ToString();
        string proposedPath = string.IsNullOrEmpty(InitialCatalog)
            ? NewCatalogName
            : $"{InitialCatalog}{separator}{NewCatalogName}";

        var catalogs = AvailableCatalogs ?? Enumerable.Empty<string>();

        bool exists = catalogs.Contains(proposedPath, StringComparer.OrdinalIgnoreCase);
        bool isSelf = !IsNew && string.Equals(proposedPath, GetCurrentFullPath(), StringComparison.OrdinalIgnoreCase);

        if (exists && !isSelf)
        {
            AddError(propertyName, ViewResourceLoader.GetString("ValidationError_CatalogExists"));
        }

        OnPropertyChanged(nameof(NameError));
    }

    /// <summary>
    /// Adds an error message for a specific property and triggers the ErrorsChanged event.
    /// </summary>
    /// <param name="propertyName">The property name to associate the error with.</param>
    /// <param name="error">The error message to add.</param>
    private void AddError(string propertyName, string error)
    {
        if (!_errors.ContainsKey(propertyName))
            _errors[propertyName] = [];

        if (!_errors[propertyName].Contains(error))
        {
            _errors[propertyName].Add(error);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Removes all errors for a specific property and triggers the ErrorsChanged event.
    /// </summary>
    /// <param name="propertyName">The property name to clear errors for.</param>
    private void ClearErrors(string propertyName)
    {
        if (_errors.Remove(propertyName))
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Determines if the Save command can be executed based on validation state and name presence.
    /// </summary>
    /// <returns>True if no errors exist and NewCatalogName is not empty or whitespace.</returns>
    public bool CanSave()
    {
        return !HasErrors && !string.IsNullOrWhiteSpace(NewCatalogName);
    }

    /// <summary>
    /// Constructs and returns the full catalog path of the currently selected catalog.
    /// </summary>
    /// <returns>The full path as a string, or empty if no catalog is selected.</returns>
    private string GetCurrentFullPath()
    {
        if (_selectedCatalog == null) return string.Empty;
        return string.IsNullOrEmpty(_selectedCatalog.CatalogPath)
            ? _selectedCatalog.Name
            : $"{_selectedCatalog.CatalogPath}{AppConstants.CatalogDelimeter}{_selectedCatalog.Name}";
    }

    /// <summary>
    /// Initializes the view model with parameters from the dialog context.
    /// </summary>
    /// <param name="editCatalogParameters">Contains selected catalog, existing catalogs, and default values.</param>
    public void Initialize(EditCatalogParameters editCatalogParameters)
    {
        editCatalogParameters ??= new([], string.Empty, null, []);

        _selectedCatalog = editCatalogParameters.SelectedCatalog;
        _existingCatalogs = editCatalogParameters.ExistingCatalogs;
        IsNew = editCatalogParameters.SelectedCatalog is null;

        var allCatalogs = editCatalogParameters.AllAvailableCatalogs ?? [];

        if (!IsNew && _selectedCatalog != null)
        {
            string currentPath = GetCurrentFullPath();
            string childPrefix = $"{currentPath}{AppConstants.CatalogDelimeter}";

            AvailableCatalogs = [.. allCatalogs
                .Where(c => !string.Equals(c, currentPath, StringComparison.OrdinalIgnoreCase) &&
                            !c.StartsWith(childPrefix, StringComparison.OrdinalIgnoreCase))];
        }
        else
        {
            AvailableCatalogs = allCatalogs;
        }

        NewCatalogName = editCatalogParameters.SelectedCatalog?.Name ?? string.Empty;
        InitialCatalog = editCatalogParameters.SelectedCatalog?.CatalogPath ?? editCatalogParameters.DefaultCatalog ?? string.Empty;

        ValidateName();
    }

    /// <summary>
    /// Handles cancel action. Does nothing but returns a completed task for async compatibility.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task OnCancelAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves the catalog (create or update) based on IsNew flag. Logs success/failure with proper severity.
    /// </summary>
    /// <returns>A task representing the async save operation.</returns>
    public async Task OnSaveAsync()
    {
        ValidateName();
        if (HasErrors) return;

        if (IsNew)
        {
            ErrorOr<PangoPasswordDto> result = await _sender.Send(new NewPasswordCommand(NewCatalogName, Guid.NewGuid().ToString(), Guid.NewGuid().ToString()) { IsCatalogHolder = true, CatalogPath = InitialCatalog });

            if (result.IsError)
            {
                if (Logger.IsEnabled(LogLevel.Error))
                    Logger.LogError("Creating catalog \"{NewCatalogName}\" failed: {FirstError}", NewCatalogName, result.FirstError);

                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                    string.Format(ViewResourceLoader.GetString("CatalogCreationFailed_Format"), NewCatalogName),
                    Core.Enums.AppNotificationType.Warning));
            }
            else
            {
                var entity = result.Value.Adapt<PangoPasswordListItemDto>();
                WeakReferenceMessenger.Default.Send(new PasswordCreatedMessage(entity));
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                    string.Format(ViewResourceLoader.GetString("CatalogCreated_Format"), NewCatalogName)));

                if (Logger.IsEnabled(LogLevel.Debug))
                    Logger.LogDebug("Catalog \"{NewCatalogName}\" successfully created", NewCatalogName);
            }
        }
        else
        {
            ErrorOr<PangoPasswordDto> result = await _sender.Send(new UpdatePasswordCommand(_selectedCatalog!.Id, NewCatalogName, string.Empty, string.Empty) { IsCatalogHolder = true, CatalogPath = InitialCatalog });

            if (result.IsError)
            {
                if (Logger.IsEnabled(LogLevel.Error))
                    Logger.LogError("Updating catalog \"{NewCatalogName}\" failed: {FirstError}", NewCatalogName, result.FirstError);

                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                    string.Format(ViewResourceLoader.GetString("CatalogUpdateFailed_Format"), NewCatalogName),
                    Core.Enums.AppNotificationType.Warning));
            }
            else
            {
                var entity = result.Value.Adapt<PangoPasswordListItemDto>();
                WeakReferenceMessenger.Default.Send(new PasswordUpdatedMessage(entity));
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                    string.Format(ViewResourceLoader.GetString("CatalogUpdated_Format"), NewCatalogName)));

                if (Logger.IsEnabled(LogLevel.Debug))
                    Logger.LogDebug("Catalog \"{NewCatalogName}\" successfully updated", NewCatalogName);
            }
        }
    }

    #endregion
}
