using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Data.Commands.Import;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Persistence.File;
using System;
using System.Text;
using System.Threading.Tasks;
using ImportDataValidator = Pango.Desktop.Uwp.Dialogs.Validators.ImportDataValidator;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public class ImportDialogViewModel : ViewModelBase, IDialogViewModel
{
    #region Fields

    private readonly ISender _sender;
    private ImportDataValidator _validator;
    private ImportDataParameters? _parameters;
    private readonly IPasswordHashProvider _passwordHashProvider;

    #endregion

    public ImportDialogViewModel(ISender sender, IPasswordHashProvider passwordHashProvider, ILogger<ImportDialogViewModel> logger) : base(logger)
    {
        DialogContext = new DialogContext();

        _sender = sender;
        _validator = new();
        _validator.ErrorsChanged += Validator_ErrorsChanged;
        _passwordHashProvider = passwordHashProvider;
    }

    #region Properties

    public IDialogContext DialogContext { get; }

    public ImportDataValidator Validator 
    {
        get => _validator;
        set => SetProperty(ref _validator, value);
    }

    #endregion

    #region Overrides

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        ResetDialog();

        if (parameter is ImportDataParameters dialogParameters)
        {
            _parameters = dialogParameters;
        }
    }

    #endregion

    #region Public Methods

    public bool CanSave()
    {
        return !Validator.HasErrors;
    }

    public Task OnCancelAsync()
    {
        return Task.CompletedTask;
    }

    public async Task OnSaveAsync()
    {
        if (_parameters is null)
        {
            Logger.LogError("Cannot do the import: ImportDataParameters is null");
            return;
        }

        byte[]? saltBytes = Encoding.UTF8.GetBytes(this.Validator.MasterPassword);
        Array.Resize(ref saltBytes, 16);

        string passwordHash = _passwordHashProvider.Hash(Validator.MasterPassword, saltBytes);
        EncodingOptions encoding = new (passwordHash, Convert.ToBase64String(saltBytes));

        ErrorOr<ImportResult> result = await _sender.Send(new ImportDataCommand(_parameters.FilePath, new ImportOptions(encoding)));

        if (result.IsError)
        {
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage($"Import failed: {result.FirstError}", Core.Enums.AppNotificationType.Error));
        }
        else
        {
            int count = result.Value.Manifest.Contents[Domain.Enums.ContentType.Passwords];
            string message = count == 1 ? ViewResourceLoader.GetString("ImportSingleCompleted_Message")
                : string.Format(ViewResourceLoader.GetString("ImportCompleted_Message"), count);

            WeakReferenceMessenger.Default.Send<InAppNotificationMessage>(new InAppNotificationMessage(message));
            WeakReferenceMessenger.Default.Send<ImportCompletedMessage>(new ImportCompletedMessage(result.Value));
        }
    }

    #endregion

    #region Private Methods

    private void ResetDialog()
    {
        if (Validator != null)
        {
            Validator.ErrorsChanged -= Validator_ErrorsChanged;
        }

        Validator = new();
        Validator.ErrorsChanged += Validator_ErrorsChanged;

        // raise this to make sure that the command "can execute" predicate is triggerred too
        DialogContext.RaiseDialogContentChanged();
    }

    private void Validator_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
    {
        DialogContext.RaiseDialogContentChanged(e);
    }

    #endregion
}
