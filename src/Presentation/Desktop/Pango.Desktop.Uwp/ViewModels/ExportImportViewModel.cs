using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Application.UseCases.Data.Commands.Export;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Extensions;
using Pango.Desktop.Uwp.Dialogs;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.ExportImport)]
public sealed class ExportImportViewModel : ViewModelBase
{
    #region Fields

    private readonly ISender _sender;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IDialogService _dialogService;
    private int _selectedOption;
    private string _titleText = string.Empty;
    private string _importFilePath = string.Empty;

    #endregion

    public ExportImportViewModel(
        ILogger<ExportImportViewModel> logger, 
        ISender sender, 
        IUserContextProvider userContextProvider, 
        IDialogService dialogService) 
        : base(logger)
    {
        ExportDataCommand = new RelayCommand(OnExportAsync);
        ImportDataCommand = new RelayCommand(OnImportDataAsync, CanImport);
        NavigateToOptionCommand = new RelayCommand<int>(async(e) => await OnNavigateToOptionAsync(e));

        _sender = sender;
        _userContextProvider = userContextProvider;
        _dialogService = dialogService;

        Passwords = [];
        TitleText = ViewResourceLoader.GetString("ExportAndImportTooltip");
    }


    #region Commands

    public RelayCommand ExportDataCommand { get; }
    public RelayCommand ImportDataCommand { get; }
    public RelayCommand<int> NavigateToOptionCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<PangoExplorerItem> Passwords { get; private set; }

    /// <summary>
    /// Path to the pango archive that's gonna be imported
    /// </summary>
    public string ImportFilePath
    {
        get => _importFilePath;
        set
        {
            SetProperty(ref _importFilePath, value);
            OnPropertyChanged(nameof(ImportDataCommand));
        }
    }

    /// <summary>
    /// The view title
    /// </summary>
    public string TitleText
    {
        get => _titleText;
        set => SetProperty(ref _titleText, value);
    }

    /// <summary>
    /// Selected view tab: general, import or export
    /// </summary>
    public int SelectedOption
    {
        get => _selectedOption;
        set 
        {
            SetProperty(ref _selectedOption, value);

            TitleText = value switch
            {
                1 => ViewResourceLoader.GetString("ExportTitle"),
                2 => ViewResourceLoader.GetString("ImportTitle"),
                _ => ViewResourceLoader.GetString("ExportAndImportTooltip")
            };
        }
    }

    #endregion

    #region Overrides

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        await ResetViewAsync();
    }

    protected override void RegisterMessages()
    {
        base.RegisterMessages();
        WeakReferenceMessenger.Default.Register<ExportCompletedMessage>(this, OnExportCompleted);
        WeakReferenceMessenger.Default.Register<ImportCompletedMessage>(this, OnImportCompleted);
    }

    protected override void UnregisterMessages()
    {
        base.UnregisterMessages();
        WeakReferenceMessenger.Default.Unregister<ExportCompletedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ImportCompletedMessage>(this);
    }

    #endregion

    #region Private Methods

    private bool CanImport()
    {
        return !string.IsNullOrWhiteSpace(ImportFilePath);
    }

    private async void OnImportCompleted(object recipient, ImportCompletedMessage message)
    {
        await ResetViewAsync();
    }

    private async void OnExportCompleted(object recipient, ExportCompletedMessage message)
    {
         await _dialogService.ShowExportResultDialogAsync(new ExportResultParameters(message.Value));
    }

    /// <summary>
    /// Resets the entire view
    /// </summary>
    /// <returns></returns>
    private async Task ResetViewAsync()
    {
        await OnNavigateToOptionAsync(0);
        IEnumerable<PangoExplorerItem> passwords = await LoadPasswordsAsync();
        DisplayPasswordsInTree(passwords);
    }

    /// <summary>
    /// Opens a dialog to import items from selected archive, located at <see cref="ImportFilePath"/>
    /// </summary>
    private async void OnImportDataAsync()
    {
        await _dialogService.ShowDataImportDialogAsync(new ImportDataParameters(ImportFilePath));
    }

    /// <summary>
    /// Opens a dialog to export selected items
    /// </summary>
    private async void OnExportAsync()
    {
        await _dialogService.ShowDataExportDialogAsync(new ExportDataParameters(PrepareContent()));
    }

    /// <summary>
    /// Converts all selected <see cref="PangoExplorerItem"/> from <see cref="Passwords"/> into a list of <see cref="ExportItem"/>
    /// </summary>
    /// <returns></returns>
    private List<ExportItem> PrepareContent()
    {
        var selected = FindAll([.. Passwords], p => p.IsSelected || p.Children.Any(c => c.IsSelected));
        if(selected.Count == 0)
        {
            return [];
        }

        List<ExportItem> items = selected.Select(s => new ExportItem(Domain.Enums.ContentType.Passwords, s.Id)).ToList();

        return items;
    }

    /// <summary>
    /// Finds all <see cref="PangoExplorerItem"/> in <paramref name="sourceList"/> that fit <paramref name="predicate"/>
    /// </summary>
    /// <param name="sourceList"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    private List<PangoExplorerItem> FindAll(List<PangoExplorerItem> sourceList, Func<PangoExplorerItem, bool> predicate)
    {
        List<PangoExplorerItem> result = [];

        if (sourceList is null || sourceList.Count == 0)
        {
            return result;
        }

        foreach (var item in sourceList)
        {
            if (predicate(item))
            {
                result.Add(item);
            }

            var children = FindAll([.. item.Children], predicate);

            if(children.Count > 0)
            {
                result.AddRange(children);
            }
        }

        return result;
    }

    /// <summary>
    /// Handles navigation between views
    /// </summary>
    /// <param name="option">the view number: 0 - general, 1 - export, 2 - import</param>
    /// <returns></returns>
    private async Task OnNavigateToOptionAsync(int option)
    {
        switch(option)
        {
            case 0:
                SelectedOption = 0;
                await OnNavigatedToGeneralAsync();
                break;

            case 1:
                SelectedOption = 1;
                await OnNavigatedToExportAsync();
                break;

            case 2:
                SelectedOption = 2;
                await OnNavigatedToImportAsync();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Loads passwords and returns a list of that
    /// </summary>
    /// <returns></returns>
    private async Task<IEnumerable<PangoExplorerItem>> LoadPasswordsAsync()
    {
        Logger.LogDebug($"Loading passwords...");
        var queryResult = await _sender.Send<ErrorOr<IEnumerable<PangoPasswordListItemDto>>>(new UserPasswordsQuery());

        if (queryResult.IsError)
        {
            Logger.LogError("Loaded passwords failed: {FirstError}", queryResult.FirstError);
            return [];
        }
        else
        {
            Logger.LogDebug("Loaded {count} passwords", queryResult.Value.Count(p => !p.IsCatalog));
            return queryResult.Value.Adapt<IEnumerable<PangoExplorerItem>>().OrderBy(i => i.NestingLevel);
        }
    }

    /// <summary>
    /// Displays <paramref name="passwords"/> in a tree view
    /// </summary>
    /// <param name="passwords"></param>
    private void DisplayPasswordsInTree(IEnumerable<PangoExplorerItem> passwords)
    {
        Passwords.Clear();
        foreach (var pwd in passwords)
        {
            AddPassword(Passwords, pwd, pwd.CatalogPath.ParseCatalogPath());
        }
    }

    /// <summary>
    /// Adds a <paramref name="password"/> to the tree collection of <paramref name="passwords"/>
    /// </summary>
    /// <param name="passwords"></param>
    /// <param name="password"></param>
    /// <param name="catalogs"></param>
    /// <returns></returns>
    private bool AddPassword(ObservableCollection<PangoExplorerItem> passwords, PangoExplorerItem password, Queue<string>? catalogs)
    {
        if (catalogs is null || catalogs.Count == 0)
        {
            Insert(passwords, password);
            return true;
        }

        string catalogName = catalogs.Dequeue();
        PangoExplorerItem? catalog = passwords.FirstOrDefault(p => p.Type == PangoExplorerItem.ExplorerItemType.Folder && p.Name == catalogName);

        if (catalog is null)
        {
            return false;
        }

        if (catalogs.Any())
        {
            return AddPassword(catalog.Children, password, catalogs);
        }
        else
        {
            password.Parent = catalog;
            Insert(catalog.Children, password);
            return true;
        }
    }

    /// <summary>
    /// Inserts <paramref name="passwordToInsert"/> into sorted <paramref name="sortedPasswordsList"/>
    /// </summary>
    /// <param name="sortedPasswordsList">already sorted collection of passwords</param>
    /// <param name="passwordToInsert">a password to add into the <paramref name="sortedPasswordsList"/></param>
    private static void Insert(ObservableCollection<PangoExplorerItem> sortedPasswordsList, PangoExplorerItem passwordToInsert)
    {
        if (!sortedPasswordsList.Any())
        {
            sortedPasswordsList.Add(passwordToInsert);
            return;
        }

        int index = 0;

        if (passwordToInsert.Type == PangoExplorerItem.ExplorerItemType.File)
        {
            for (; index < sortedPasswordsList.Count; index++)
            {
                if (sortedPasswordsList[index].Type == PangoExplorerItem.ExplorerItemType.File)
                {
                    break;
                }
            }
        }

        for (; index < sortedPasswordsList.Count; index++)
        {
            if (sortedPasswordsList[index].Type == PangoExplorerItem.ExplorerItemType.File && passwordToInsert.Type == PangoExplorerItem.ExplorerItemType.Folder)
            {
                break;
            }

            if (sortedPasswordsList[index].Name.CompareTo(passwordToInsert.Name) < 0)
            {
                continue;
            }
            else
            {
                break;
            }
        }

        sortedPasswordsList.Insert(index, passwordToInsert);
    }

    private Task OnNavigatedToGeneralAsync()
    {
        return Task.CompletedTask;
    }

    private async Task OnNavigatedToExportAsync()
    {
        IEnumerable<PangoExplorerItem> passwords = await LoadPasswordsAsync();
        DisplayPasswordsInTree(passwords);
    }

    private Task OnNavigatedToImportAsync()
    {
        ImportFilePath = string.Empty;

        return Task.CompletedTask;
    }
    #endregion
}
