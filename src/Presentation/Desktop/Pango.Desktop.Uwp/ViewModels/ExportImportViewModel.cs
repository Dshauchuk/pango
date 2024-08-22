using CommunityToolkit.Mvvm.Input;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Extensions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Application.UseCases.Data.Commands.Export;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Persistence.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.ExportImport)]
public sealed class ExportImportViewModel : ViewModelBase
{
    #region Fields

    private readonly ISender _sender;
    private readonly IUserContextProvider _userContextProvider;
    private int _selectedOption;

    #endregion

    public ExportImportViewModel(ILogger<ExportImportViewModel> logger, ISender sender, IUserContextProvider userContextProvider) : base(logger)
    {
        ExportDataCommand = new RelayCommand(OnExportDataAsync);
        ImportDataCommand = new RelayCommand(OnImportDataAsync);
        NavigateToOptionCommand = new RelayCommand<int>(OnNavigateToOption);
        _sender = sender;
        _userContextProvider = userContextProvider;
    }


    #region Commands

    public RelayCommand ExportDataCommand { get; }
    public RelayCommand ImportDataCommand { get; }
    public RelayCommand<int> NavigateToOptionCommand { get; }

    #endregion

    #region Properties


    public int SelectedOption
    {
        get => _selectedOption;
        set => SetProperty(ref _selectedOption, value);
    }

    #endregion

    #region Private Methods

    private async void OnImportDataAsync()
    {
        

    }

    private async void OnExportDataAsync()
    {
        ErrorOr<ExportResult> result = 
            await _sender.Send(new ExportDataCommand(await PrepareContentAsync(), new ExportOptions(await _userContextProvider.GetEncodingOptionsAsync())));
    }

    private async Task<List<IContentPackage>> PrepareContentAsync()
    {
        var queryResult = await _sender.Send<ErrorOr<IEnumerable<PangoPasswordListItemDto>>>(new UserPasswordsQuery());

        List<IContentPackage> fileContents = new(100);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        string userName = "tmp";

        foreach (var chunk in queryResult.Value.ToList().ChunkBy(2))
        {
            ContentPackage fileContent = new(userName, Domain.Enums.ContentType.Passwords, chunk.GetType().FullName ?? string.Empty, chunk.Count, chunk, now);
            fileContents.Add(fileContent);
        }

        return fileContents;
    }

    private void OnNavigateToOption(int option)
    {
        SelectedOption = option switch
        {
            0 => 0,
            1 => 1,
            2 => 2,
            _ => throw new InvalidOperationException($"Option {option} cannot be navigated")
        };
    }

    #endregion

}
