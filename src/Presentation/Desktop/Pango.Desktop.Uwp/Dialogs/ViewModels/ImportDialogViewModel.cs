using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Persistence.File;
using ImportDataValidator = Pango.Desktop.Uwp.Dialogs.Validators.ImportDataValidator;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public partial class ImportDialogViewModel : ViewModelBase, IDialogViewModel
{
    #region Fields

    private ImportDataValidator _validator;
    private ImportDataParameters? _parameters;
    private readonly ISender sender;
    private readonly IDataImporter _dataImporter;
    private bool _isInitialized = false;

    #endregion

    public ImportDialogViewModel(ISender sender, IDataImporter dataImporter, ILogger<ImportDialogViewModel> logger) : base(logger)
    {
        DialogContext = new DialogContext();
        this.sender = sender;
        _dataImporter = dataImporter;
        _validator = new();
        _validator.ErrorsChanged += Validator_ErrorsChanged;
    }

    #region Properties

    public IDialogContext DialogContext { get; }
    public ImportDataValidator Validator { get => _validator; set => SetProperty(ref _validator, value); }

    #endregion

    #region Overrides

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        if (_isInitialized && parameter == null)
            return;

        ResetDialog();

        if (parameter is ImportDataParameters dialogParameters)
        {
            _parameters = dialogParameters;
            _isInitialized = true;
        }
    }

    #endregion

    #region Public Methods

    public bool CanSave() => !Validator.HasErrors;
    public Task OnCancelAsync() => Task.CompletedTask;

    public async Task OnSaveAsync()
    {
        Logger.LogInformation("Validating import archive password.");
        if (_parameters is null) return;

        string password = Validator.MasterPassword?.Trim() ?? string.Empty;
        var encoding = new EncodingOptions(password, string.Empty);

        try
        {
            // Try to decrypt content to validate password and get items
            var options = new ImportOptions(encoding);
            var content = await _dataImporter.ExtractContentAsync(_parameters.FilePath, options);

            if (content == null || content.Count == 0)
            {
                Logger.LogWarning("Import failed: Invalid password or corrupted archive.");
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                    "Invalid password or corrupted archive", Core.Enums.AppNotificationType.Error));
                return;
            }

            List<Domain.Entities.PangoPassword> allItems = [];
            foreach (var package in content)
            {
                if (package.Data is IEnumerable<Domain.Entities.PangoPassword> items)
                    allItems.AddRange(items);
                else if (package.Data is Newtonsoft.Json.Linq.JArray jArray)
                {
                    var fallback = jArray.ToObject<List<Domain.Entities.PangoPassword>>();
                    if (fallback != null) allItems.AddRange(fallback);
                }
            }

            // Create parameters with decrypted content and password
            var selectiveParams = new ImportDataParametersWithPassword(_parameters.FilePath, password, allItems);

            // Request navigation to the Selection Dialog
            WeakReferenceMessenger.Default.Send(new NavigationRequestedMessage(
                new NavigationParameters(Core.Enums.AppView.ExportImport, Core.Enums.AppView.ExportImport, selectiveParams)
            ));

            // Notify UI to open the specific dialog
            WeakReferenceMessenger.Default.Send(new ImportPreviewReadyMessage(selectiveParams));
        }
        catch (Exception)
        {
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                ViewResourceLoader.GetString("Import_EncryptedArchiveError"), Core.Enums.AppNotificationType.Error));
        }
    }

    #endregion

    #region Private Methods

    private void ResetDialog()
    {
        Validator?.ErrorsChanged -= Validator_ErrorsChanged;
        Validator = new();
        Validator.ErrorsChanged += Validator_ErrorsChanged;

        // raise this to make sure that the command "can execute" predicate is triggerred too
        DialogContext.RaiseDialogContentChanged();
    }

    private void Validator_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
        => DialogContext.RaiseDialogContentChanged(e);

    #endregion
}

// Message to trigger the second dialog
public class ImportPreviewReadyMessage(ImportDataParameters parameters) : CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<ImportDataParameters>(parameters);