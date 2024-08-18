using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using System;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.ExportImport)]
public sealed class ExportImportViewModel : ViewModelBase
{
    #region Fields

    private int _selectedOption;

    #endregion

    public ExportImportViewModel(ILogger<ExportImportViewModel> logger) : base(logger)
    {
        ExportDataCommand = new RelayCommand(OnExportDataAsync);
        ImportDataCommand = new RelayCommand(OnImportDataAsync);
        NavigateToOptionCommand = new RelayCommand<int>(OnNavigateToOption);
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
