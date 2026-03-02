using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Application.UseCases.Data.Commands.Export;
using Pango.Application.UseCases.Data.Commands.Import;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Extensions;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Dialogs;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.ExportImport)]
public sealed partial class ExportImportViewModel : ViewModelBase
{
    #region Fields

    private readonly ISender _sender;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IDialogService _dialogService;
    private int _selectedOption;
    private string _titleText = string.Empty;
    private string _importFilePath = string.Empty;
    private string _decryptedPasswordCache = string.Empty;

    #endregion

    public ExportImportViewModel(
        ILogger<ExportImportViewModel> logger,
        ISender sender,
        IUserContextProvider userContextProvider,
        IDialogService dialogService)
        : base(logger)
    {
        ExportDataCommand = new RelayCommand(OnExportAsync, CanExport);
        ReadImportFileCommand = new RelayCommand(OnReadImportFileAsync, CanReadImportFile); // Step 1
        FinalizeImportCommand = new RelayCommand(OnFinalizeImportAsync, CanFinalizeImport); // Step 3
        NavigateToOptionCommand = new RelayCommand<int>(async (e) => await OnNavigateToOptionAsync(e));

        _sender = sender;
        _userContextProvider = userContextProvider;
        _dialogService = dialogService;

        Passwords = []; // Export Tree Items
        ImportItems = []; // Import Tree Items

        TitleText = ViewResourceLoader.GetString("ExportAndImportTooltip");
    }


    #region Commands

    public RelayCommand ExportDataCommand { get; }

    // Step 1: Open Password Dialog
    public RelayCommand ReadImportFileCommand { get; }

    // Step 3: Execute Import
    public RelayCommand FinalizeImportCommand { get; }

    public RelayCommand<int> NavigateToOptionCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<PangoExplorerItem> Passwords { get; private set; }

    // Collection for the Import TreeView (Pivot Item 3)
    public ObservableCollection<PangoExplorerItem> ImportItems { get; private set; }

    /// <summary>
    /// Path to the pango archive that's gonna be imported
    /// </summary>
    public string ImportFilePath
    {
        get => _importFilePath;
        set
        {
            SetProperty(ref _importFilePath, value);
            OnPropertyChanged(nameof(ReadImportFileCommand));
            ReadImportFileCommand.NotifyCanExecuteChanged();
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
    /// Selected view tab: 0-general, 1-export, 2-import file, 3-import selection
    /// </summary>
    public int SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (SetProperty(ref _selectedOption, value))
            {
                TitleText = value switch
                {
                    1 => ViewResourceLoader.GetString("ExportTitle"),
                    2 => ViewResourceLoader.GetString("ImportTitle"),
                    3 => ViewResourceLoader.GetString("SelectItemsToImport"),
                    _ => ViewResourceLoader.GetString("ExportAndImportTooltip")
                };
            }
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
        WeakReferenceMessenger.Default.Register<SelectionChangedMessage>(this, (r, m) =>
        {
            App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                ExportDataCommand.NotifyCanExecuteChanged();
                FinalizeImportCommand.NotifyCanExecuteChanged();
            });
        });
    }

    protected override void UnregisterMessages()
    {
        base.UnregisterMessages();
        WeakReferenceMessenger.Default.Unregister<ExportCompletedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ImportCompletedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<SelectionChangedMessage>(this);
    }

    #endregion

    #region Private Methods

    private bool CanExport() => Passwords.Any(p => p.IsSelected);
    private bool CanReadImportFile() => !string.IsNullOrWhiteSpace(ImportFilePath);
    private bool CanFinalizeImport() => ImportItems.FindItems(x => x.IsSelected).Count != 0;

    private async void OnImportCompleted(object recipient, ImportCompletedMessage message)
    {
        await ResetViewAsync();
    }

    private async void OnExportCompleted(object recipient, ExportCompletedMessage message)
    {
        await ResetViewAsync();
        await _dialogService.ShowExportResultDialogAsync(new ExportResultParameters(message.Value));
    }

    /// <summary>
    /// Resets the entire view
    /// </summary>
    /// <returns></returns>
    private async Task ResetViewAsync()
    {
        await OnNavigateToOptionAsync(0);
        _decryptedPasswordCache = string.Empty;
        ImportItems.Clear();
        ImportFilePath = string.Empty;

        // Reload export data in background to keep UI fresh
        var passwords = await LoadPasswordsAsync();
        DisplayPasswordsInTree(passwords);
    }

    // Step 1: Open Password Dialog
    private async void OnReadImportFileAsync()
    {
        await _dialogService.ShowDataImportDialogAsync(new ImportDataParameters(ImportFilePath));
    }

    // Step 2: Called by View when password is correct (via Messenger)
    public async Task HandleImportPreviewAsync(ImportDataParameters parameters)
    {
        if (parameters is ImportDataParametersWithPassword p)
        {
            _decryptedPasswordCache = p.Password;

            ImportItems.Clear();

            // Build tree from the decrypted content passed from dialog
            var tree = TreeBuilder.BuildTree([.. p.PreLoadedContent]);
            foreach (var item in tree)
            {
                ImportItems.Add(item);
            }

            // Move to Selection Tab
            await OnNavigateToOptionAsync(3);
        }
    }

    // Step 3: Execute Import
    private async void OnFinalizeImportAsync()
    {
        IsBusy = true;
        try
        {
            var selectedIds = ImportItems.FindItems(x => x.IsSelected).Select(x => x.Id).ToList();

            // Read Import Destination Setting
            bool importToFolder = false;
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("ImportDestination", out object? val))
            {
                if (val is int i) importToFolder = i == 1;
                else if (val is bool b) importToFolder = b;
            }

            var encoding = new EncodingOptions(_decryptedPasswordCache, "");
            var options = new Pango.Persistence.File.ImportOptions(encoding);

            var cmd = new ImportDataCommand(ImportFilePath, options, selectedIds, importToFolder);
            var result = await _sender.Send(cmd);

            if (result.IsError)
            {
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(result.FirstError.Description, Core.Enums.AppNotificationType.Error));
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(ViewResourceLoader.GetString("ImportCompleted_Message")));
                WeakReferenceMessenger.Default.Send(new ImportCompletedMessage(result.Value));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Import finalization failed");
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage("Import failed", Core.Enums.AppNotificationType.Error));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void OnExportAsync()
    {
        await _dialogService.ShowDataExportDialogAsync(new ExportDataParameters(PrepareContent()));
    }

    private List<ExportItem> PrepareContent()
    {
        var selected = FindAll([.. Passwords], p => p.IsSelected || p.Children.Any(c => c.IsSelected));
        if (selected.Count == 0) return [];
        return [.. selected.Select(s => new ExportItem(Domain.Enums.ContentType.Passwords, s.Id))];
    }

    private static List<PangoExplorerItem> FindAll(List<PangoExplorerItem> sourceList, Func<PangoExplorerItem, bool> predicate)
    {
        List<PangoExplorerItem> result = [];
        if (sourceList is null || sourceList.Count == 0) return result;

        foreach (var item in sourceList)
        {
            if (predicate(item)) result.Add(item);
            var children = FindAll([.. item.Children], predicate);
            if (children.Count > 0) result.AddRange(children);
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
        SelectedOption = option;

        switch (option)
        {
            case 0: // General Menu
                await OnNavigatedToGeneralAsync();
                break;
            case 1: // Export
                await OnNavigatedToExportAsync();
                break;
            case 2: // Import File
                await OnNavigatedToImportAsync();
                break;
            case 3: // Import Selection
                // Data is already loaded by HandleImportPreviewAsync
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
    private static bool AddPassword(ObservableCollection<PangoExplorerItem> passwords, PangoExplorerItem password, Queue<string>? catalogs)
    {
        if (catalogs is null || catalogs.Count == 0)
        {
            Insert(passwords, password);
            return true;
        }

        string catalogName = catalogs.Dequeue();
        PangoExplorerItem? catalog = passwords.FirstOrDefault(p => p.Type == PangoExplorerItem.ExplorerItemType.Folder && p.Name == catalogName);

        if (catalog is null) return false;

        if (catalogs.Count != 0)
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
                if (sortedPasswordsList[index].Type == PangoExplorerItem.ExplorerItemType.File) break;
            }
        }

        for (; index < sortedPasswordsList.Count; index++)
        {
            if (sortedPasswordsList[index].Type == PangoExplorerItem.ExplorerItemType.File && passwordToInsert.Type == PangoExplorerItem.ExplorerItemType.Folder) break;
            if (sortedPasswordsList[index].Name.CompareTo(passwordToInsert.Name) < 0) continue;
            else break;
        }

        sortedPasswordsList.Insert(index, passwordToInsert);
    }

    private Task OnNavigatedToGeneralAsync()
    {
        ImportFilePath = string.Empty;
        ImportItems.Clear();
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
        ImportItems.Clear();
        return Task.CompletedTask;
    }

    #endregion
}
