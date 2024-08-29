using CommunityToolkit.Mvvm.ComponentModel;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Data.Commands.Export;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Persistence.File;
using System;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public class ExportDataValidator : ObservableValidator
{
    private string _owner
}

public class ExportDialogViewModel : ViewModelBase, IDialogViewModel
{
    #region Fields

    private readonly ISender _sender;
    private readonly IUserContextProvider _userContextProvider;
    private ExportDataParameters? _parameters;

    private string _owner;
    private string _masterPassword;
    private string _exportingItemsInfo;

    #endregion

    public ExportDialogViewModel(ILogger<ExportDialogViewModel> logger, ISender sender, IUserContextProvider userContextProvider)
        : base(logger)
    {
        DialogContext = new DialogContext();
        _sender = sender;
        _userContextProvider = userContextProvider;
    }

    #region Properties

    public string Owner
    {
        get => _owner;
        set => SetProperty(ref _owner, value);
    }

    public string MasterPassword
    {
        get => _masterPassword;
        set => SetProperty(ref _masterPassword, value);
    }

    public string ExportingItemsInfo
    {
        get => _exportingItemsInfo;
        set => SetProperty(ref _exportingItemsInfo, value);
    }

    #endregion


    public IDialogContext DialogContext { get; }

    public bool CanSave()
    {
        return true;
    }

    public Task OnCancelAsync()
    {
        return Task.CompletedTask;
    }

    public async Task OnSaveAsync()
    {
        var t = await _userContextProvider.GetEncodingOptionsAsync();

        var a = Convert.FromBase64String(t.Key);
        var b = a[..16];

        string tmpUser = "tmp";
        var encoding = new EncodingOptions(t.Key, Convert.ToBase64String(b));

        ErrorOr<ExportResult> result =
            await _sender.Send(new ExportDataCommand(_parameters.Items, new ExportOptions(tmpUser, encoding)));
    }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        if(parameter is not ExportDataParameters dialogParameters)
        {
            return;
        }

        _parameters = dialogParameters;


        ExportingItemsInfo = $"{_parameters.Items.Count} passwords";
    }
}
