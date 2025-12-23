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
    private const string FileExtension = ".pngx";
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
        if (Validator.HasErrors)
        {
            return;
        }

        if (_parameters is null)
        {
            Logger.LogError("Cannot do the export: ExportDataParameters is null");
            return;
        }

        var saltBytes = Encoding.UTF8.GetBytes(Validator.MasterPassword);
        Array.Resize(ref saltBytes, 16);
        string passwordHash = _passwordHashProvider.Hash(Validator.MasterPassword, saltBytes);
        var encoding = new EncodingOptions(passwordHash, Convert.ToBase64String(saltBytes));

        ErrorOr<ExportResult> result = await _sender.Send(new ExportDataCommand(_parameters.Items, new ExportOptions(Validator.Description, encoding)));

        if (result.IsError)
        {
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage($"Export failed: {result.FirstError}", Core.Enums.AppNotificationType.Error));
        }
        else
        {
            var sourcePath = result.Value.Path;
            string fullDestinationPath = Path.Combine(Validator.ExportFolderPath, $"{Validator.FileName}{FileExtension}");

            try
            {
                File.Copy(sourcePath, fullDestinationPath, false);

                var newResult = new ExportResult(
                    fullDestinationPath,
                    result.Value.Contents,
                    result.Value.GeneratedAt,
                    result.Value.AppVersion
                );

                WeakReferenceMessenger.Default.Send<ExportCompletedMessage>(new ExportCompletedMessage(newResult));

                if (File.Exists(sourcePath))
                {
                    File.Delete(sourcePath);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to copy exported file to destination folder");
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage($"Failed to save file to selected folder: {ex.Message}", Core.Enums.AppNotificationType.Error));
                WeakReferenceMessenger.Default.Send<ExportCompletedMessage>(new ExportCompletedMessage(result.Value));
            }
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
            string defaultExportPath = Path.Combine(documentsPath, "PangoExports");

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
