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
using System.Text;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public class ExportDialogViewModel : ViewModelBase, IDialogViewModel
{
    #region Fields

    private readonly ISender _sender;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IPasswordHashProvider _passwordHashProvider;
    private ExportDataParameters? _parameters;

    private string _exportingItemsInfo = string.Empty;
    private ExportDataValidator _validator;

    #endregion

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

        _validator = new();
        _validator.ErrorsChanged += Validator_ErrorsChanged;
    }

    #region Properties

    public string ExportingItemsInfo
    {
        get => _exportingItemsInfo;
        set => SetProperty(ref _exportingItemsInfo, value);
    }

    public ExportDataValidator Validator
    {
        get => _validator;
        set => SetProperty(ref _validator, value);
    }

    public IDialogContext DialogContext { get; }

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
        if(_parameters is null)
        {
            Logger.LogError("Cannot do the export: ExportDataParameters is null");
            return;
        }

        var saltBytes = Encoding.UTF8.GetBytes(Validator.MasterPassword);
        Array.Resize(ref saltBytes, 16);

        string passwordHash = _passwordHashProvider.Hash(Validator.MasterPassword, saltBytes);
        var encoding = new EncodingOptions(passwordHash, Convert.ToBase64String(saltBytes));

        ErrorOr<ExportResult> result =
            await _sender.Send(new ExportDataCommand(_parameters.Items, new ExportOptions(Validator.Description, encoding)));

        if(result.IsError)
        {
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage($"Export failed: {result.FirstError}", Core.Enums.AppNotificationType.Error));
        }
        else
        {
            WeakReferenceMessenger.Default.Send<ExportCompletedMessage>(new ExportCompletedMessage(result.Value));            
        }
    }

    #endregion

    #region Overrides

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

    private void ResetDialog()
    {
        if(Validator != null)
        {
            Validator.ErrorsChanged -= Validator_ErrorsChanged;
        }

        Validator = new();
        Validator.ErrorsChanged += Validator_ErrorsChanged;
    }

    private void Validator_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
    {
        DialogContext.RaiseDialogContentChanged(e);
    }

    #endregion
}
