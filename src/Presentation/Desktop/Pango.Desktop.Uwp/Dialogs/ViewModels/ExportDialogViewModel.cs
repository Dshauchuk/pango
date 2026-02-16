using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Data.Commands.Export;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Dialogs.Validators;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Persistence.File;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public partial class ExportDialogViewModel : ViewModelBase, IDialogViewModel
{
    #region Fields

    private readonly ISender _sender;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IPasswordHashProvider _passwordHashProvider;
    private ExportDataParameters? _parameters;

    private string _exportFolderPath = string.Empty;
    private string _exportingItemsInfo = string.Empty;
    private ExportDataValidator _validator;

    #endregion

    // Constructor: Initializes the ViewModel with dependencies and sets up validator and defaults
    public ExportDialogViewModel(
        ILogger<ExportDialogViewModel> logger,
        ISender sender,
        IUserContextProvider userContextProvider,
        IPasswordHashProvider passwordHashProvider)
        : base(logger)
    {
        DialogContext = new DialogContext();

        _sender = sender;
        _userContextProvider = userContextProvider;
        _passwordHashProvider = passwordHashProvider;

        _validator = new ExportDataValidator();
        Validator.ErrorsChanged += Validator_ErrorsChanged;

        InitializeDefaultExportPath();
        InitializeDefaultFileName();
    }

    #region Properties

    public string ExportingItemsInfo
    {
        get => _exportingItemsInfo;
        set => SetProperty(ref _exportingItemsInfo, value);
    }

    public string ExportFolderPath
    {
        get => _exportFolderPath;
        set
        {
            if (SetProperty(ref _exportFolderPath, value))
            {
                Validator.ExportFolderPath = value;
                DialogContext.RaiseDialogContentChanged();
            }
        }
    }

    public ExportDataValidator Validator
    {
        get => _validator;
        set => SetProperty(ref _validator, value);
    }

    public IDialogContext DialogContext { get; }

    #endregion

    #region Public Methods

    // Determines if the dialog can save based on validator errors
    public bool CanSave()
    {
        return !Validator.HasErrors;
    }

    // Handles the cancel action (no-op in this implementation)
    public Task OnCancelAsync()
    {
        return Task.CompletedTask;
    }

    // Handles the save action: validates, exports data, and manages file copying
    public async Task OnSaveAsync()
    {
        Validator.Validate();
        if (Validator.HasErrors) return;
        if (_parameters is null) return;

        string masterPassword = Validator.MasterPassword;
        string description = Validator.Description;
        string exportPath = Validator.ExportFolderPath;
        string fileName = Validator.FileName;
        var itemsToExport = _parameters.Items;

        var exportResult = await Task.Run<(bool Success, string? ErrorMessage, ExportResult? Result)>(async () =>
        {
            try
            {
                byte[] staticSalt = Encoding.UTF8.GetBytes("PangoStaticExportSalt");

                byte[] derivedBytes = Rfc2898DeriveBytes.Pbkdf2(
                    masterPassword,
                    staticSalt,
                    50000,
                    HashAlgorithmName.SHA256,
                    48);

                string keyBase64 = Convert.ToBase64String(derivedBytes[0..32]);
                string ivBase64 = Convert.ToBase64String(derivedBytes[32..48]);

                var encoding = new EncodingOptions(keyBase64, ivBase64);

                ErrorOr<ExportResult> result = await _sender.Send(new ExportDataCommand(
                    itemsToExport,
                    new ExportOptions(description, encoding)
                ));

                if (result.IsError)
                {
                    return (false, result.FirstError.Description, null);
                }
                else
                {
                    var sourcePath = result.Value.Path;
                    string fullDestinationPath = Path.Combine(exportPath, $"{fileName}{AppConstants.ExportFileExtension}");

                    File.Copy(sourcePath, fullDestinationPath, true);
                    if (File.Exists(sourcePath)) File.Delete(sourcePath);

                    var finalResult = new ExportResult(fullDestinationPath, result.Value.Contents, result.Value.GeneratedAt, result.Value.AppVersion);
                    return (true, string.Empty, finalResult);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Export process failed");
                return (false, ex.Message, null);
            }
        });

        if (exportResult.Success && exportResult.Result != null)
        {
            WeakReferenceMessenger.Default.Send(new ExportCompletedMessage(exportResult.Result));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                $"Export failed: {exportResult.ErrorMessage}",
                Core.Enums.AppNotificationType.Error));
        }
    }

    #endregion

    #region Overrides

    // Called when navigating to the dialog: resets state and sets parameters
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        ResetDialog();

        if (parameter is not ExportDataParameters dialogParameters)
        {
            return;
        }

        _parameters = dialogParameters;
        ExportingItemsInfo = $"{_parameters.Items.Count} passwords";
    }

    #endregion

    #region Private Methods

    // Sets up the default export path in the user's Documents folder
    private void InitializeDefaultExportPath()
    {
        try
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string defaultExportPath = Path.Combine(documentsPath, AppConstants.DefaultExportFolderName);

            if (!Directory.Exists(defaultExportPath))
            {
                Directory.CreateDirectory(defaultExportPath);
            }

            Validator.ExportFolderPath = defaultExportPath;
            _exportFolderPath = defaultExportPath;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize default export path");
            Validator.ExportFolderPath = Environment.CurrentDirectory;
            _exportFolderPath = Environment.CurrentDirectory;
        }
    }

    // Initializes a default file name with date stamp
    private void InitializeDefaultFileName()
    {
        string defaultName = $"Pango_Backup_{DateTime.Now:yyyy-MM-dd}";
        Validator.FileName = defaultName;
    }

    // Resets the dialog state to initial configuration
    private void ResetDialog()
    {
        if (Validator is null)
        {
            Validator = new();
            Validator.ErrorsChanged += Validator_ErrorsChanged;
        }

        Validator.Reset();
        InitializeDefaultFileName();
    }

    // Handles validation errors changed event to notify dialog content changes
    private void Validator_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
    {
        DialogContext.RaiseDialogContentChanged(e);
    }

    #endregion
}
