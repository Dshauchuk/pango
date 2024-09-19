﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Data.Commands.Export;
using Pango.Application.UseCases.Data.Commands.Import;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Persistence.File;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public class ExportDataValidator : ObservableValidator
{
    private string _owner;
    private string _masterPassword;

    public ExportDataValidator()
    {
        _owner = string.Empty;
        _masterPassword = string.Empty;

        ValidateAllProperties();
    }

    [Required]
    [MinLength(3)]
    [MaxLength(20)]
    public string Owner
    {
        get => _owner;
        set
        {
            SetProperty(ref _owner, value);
            ValidateProperty(value, nameof(Owner));
        }
    }

    [Required]
    [MinLength(3)]
    public string MasterPassword
    {
        get => _masterPassword;
        set
        {
            SetProperty(ref _masterPassword, value);
            ValidateProperty(value, nameof(MasterPassword));
        }
    }
}

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

    #endregion

    public IDialogContext DialogContext { get; }

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
        string passwordHash = _passwordHashProvider.Hash(Validator.MasterPassword, out var saltBytes);
        string salt = Convert.ToBase64String(saltBytes);

        var encoding = new EncodingOptions(passwordHash, salt);

        var t = await _userContextProvider.GetEncodingOptionsAsync();
        var a = Convert.FromBase64String(t.Key);
        var b = a[..16];
        var encoding2 = new EncodingOptions(t.Key, Convert.ToBase64String(b));

        ErrorOr<ExportResult> result =
            await _sender.Send(new ExportDataCommand(_parameters.Items, new ExportOptions(Validator.Owner, encoding)));


        var t2 = await _sender.Send(new ImportDataCommand(result.Value.Path, new ImportOptions(encoding2)));

        if(result.IsError)
        {
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage($"Export failed: {result.FirstError}", Core.Enums.AppNotificationType.Error));
        }
        else
        {
            WeakReferenceMessenger.Default.Send<ExportCompletedMessage>(new ExportCompletedMessage(result.Value));            
        }
    }

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
