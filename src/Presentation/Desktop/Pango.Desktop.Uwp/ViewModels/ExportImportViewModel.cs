using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
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
using Serilog;
using System.Collections.ObjectModel;
using Windows.Storage;

namespace Pango.Desktop.Uwp.ViewModels;

/// <summary>
/// View model for handling export and import operations of password data.
/// </summary>
[AppView(AppView.ExportImport)]
public sealed partial class ExportImportViewModel : ViewModelBase
{
    #region Fields

    private readonly ISender _sender;
    private readonly IDialogService _dialogService;
    private int _selectedOption;
    private string _titleText = string.Empty;
    private string _importFilePath = string.Empty;
    private string _decryptedPasswordCache = string.Empty;
    private bool _isLoaded;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportImportViewModel"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for this view model.</param>
    /// <param name="sender">MediatR sender for dispatching commands and queries.</param>
    /// <param name="dialogService">Service for showing modal dialogs.</param>
    public ExportImportViewModel(
        ILogger<ExportImportViewModel> logger,
        ISender sender,
        IDialogService dialogService)
        : base(logger)
    {
        _sender = sender;
        _dialogService = dialogService;

        ExportDataCommand = new RelayCommand(OnExportAsync, CanExport);
        ReadImportFileCommand = new RelayCommand(OnReadImportFileAsync, CanReadImportFile);
        FinalizeImportCommand = new RelayCommand(OnFinalizeImportAsync, CanFinalizeImport);
        NavigateToOptionCommand = new AsyncRelayCommand<int>(OnNavigateToOptionAsync);

        Passwords = [];
        ImportItems = [];

        TitleText = ViewResourceLoader.GetString("ExportAndImportTooltip");

        Log.Logger?.Debug("ExportImportViewModel initialized");
    }

    #region Commands

    /// <summary>
    /// Command for exporting selected password data.
    /// </summary>
    public RelayCommand ExportDataCommand { get; }

    /// <summary>
    /// Command for opening password dialog to read import file (Step 1).
    /// </summary>
    public RelayCommand ReadImportFileCommand { get; }

    /// <summary>
    /// Command for executing the import operation (Step 3).
    /// </summary>
    public RelayCommand FinalizeImportCommand { get; }

    /// <summary>
    /// Command for navigating between export/import view options.
    /// </summary>
    public AsyncRelayCommand<int> NavigateToOptionCommand { get; }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of password items for the export tree view.
    /// </summary>
    private ObservableCollection<PangoExplorerItem> _passwords = [];

    /// <summary>
    /// Gets the collection of password items for the export tree view.
    /// </summary>
    public ObservableCollection<PangoExplorerItem> Passwords
    {
        get => _passwords;
        private set => SetProperty(ref _passwords, value);
    }

    /// <summary>
    /// Gets the collection of items for the import tree view (Pivot Item 3).
    /// </summary>
    private ObservableCollection<PangoExplorerItem> _importItems = [];

    /// <summary>
    /// Gets the collection of items for the import tree view (Pivot Item 3).
    /// </summary>
    public ObservableCollection<PangoExplorerItem> ImportItems
    {
        get => _importItems;
        private set => SetProperty(ref _importItems, value);
    }

    /// <summary>
    /// Gets or sets the file path of the .pngx archive to be imported.
    /// </summary>
    public string ImportFilePath
    {
        get => _importFilePath;
        set
        {
            if (SetProperty(ref _importFilePath, value))
            {
                Log.Logger?.Debug("ImportFilePath set to: {Path}", value);
                OnPropertyChanged(nameof(ReadImportFileCommand));
                ReadImportFileCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the view title text displayed in the UI.
    /// </summary>
    public string TitleText
    {
        get => _titleText;
        set
        {
            if (SetProperty(ref _titleText, value))
            {
                Log.Logger?.Debug("TitleText updated to: {Title}", value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected view tab index: 0=general, 1=export, 2=import file, 3=import selection.
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
                Log.Logger?.Debug("SelectedOption changed to: {Option} ({Title})", value, TitleText);
            }
        }
    }

    #endregion

    #region Overrides

    /// <summary>
    /// Called when the view is navigated to: resets view state if needed.
    /// </summary>
    /// <param name="parameter">Optional navigation parameter.</param>
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        Log.Logger?.Debug("ExportImportViewModel navigated to");

        await base.OnNavigatedToAsync(parameter);

        if (!_isLoaded || parameter != null)
        {
            Log.Logger?.Debug("Resetting view due to first load or navigation parameter");
            await ResetViewAsync();
            _isLoaded = true;
        }
    }

    /// <summary>
    /// Registers message subscriptions for selection changes and export completion events.
    /// </summary>
    protected override void RegisterMessages()
    {
        base.RegisterMessages();
        Log.Logger?.Debug("Registering message subscriptions");

        WeakReferenceMessenger.Default.Register<SelectionChangedMessage>(this, (r, m) =>
        {
            App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                ExportDataCommand.NotifyCanExecuteChanged();
                FinalizeImportCommand.NotifyCanExecuteChanged();
                Log.Logger?.Debug("SelectionChangedMessage handled: commands refreshed");
            });
        });

        WeakReferenceMessenger.Default.Register<ExportCompletedMessage>(this, async (r, m) =>
        {
            App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(async () =>
            {
                Log.Logger?.Information("ExportCompletedMessage received: showing result dialog");
                await _dialogService.ShowExportResultDialogAsync(new ExportResultParameters(m.Value));
                await OnNavigateToOptionAsync(0);
            });
        });
    }

    /// <summary>
    /// Unregisters message subscriptions to prevent memory leaks.
    /// </summary>
    protected override void UnregisterMessages()
    {
        Log.Logger?.Debug("Unregistering message subscriptions");

        base.UnregisterMessages();
        WeakReferenceMessenger.Default.Unregister<ExportCompletedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ImportCompletedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<SelectionChangedMessage>(this);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Determines if export command can execute based on selected items count.
    /// </summary>
    /// <returns>True if at least one password item is selected.</returns>
    private bool CanExport()
    {
        bool canExecute = CountSelectedRecursive(Passwords) > 0;
        Log.Logger?.Debug("CanExport: {CanExecute}", canExecute);
        return canExecute;
    }

    /// <summary>
    /// Determines if import file can be read based on file path presence.
    /// </summary>
    /// <returns>True if import file path is not empty.</returns>
    private bool CanReadImportFile()
    {
        bool canExecute = !string.IsNullOrWhiteSpace(ImportFilePath);
        Log.Logger?.Debug("CanReadImportFile: {CanExecute} (Path: {Path})", canExecute, ImportFilePath);
        return canExecute;
    }

    /// <summary>
    /// Determines if finalize import command can execute based on selected items count.
    /// </summary>
    /// <returns>True if at least one item is selected for import.</returns>
    private bool CanFinalizeImport()
    {
        bool canExecute = CountSelectedRecursive(ImportItems) > 0;
        Log.Logger?.Debug("CanFinalizeImport: {CanExecute}", canExecute);
        return canExecute;
    }

    /// <summary>
    /// Recursively counts selected non-folder items in the tree.
    /// </summary>
    /// <param name="items">Collection of tree items to evaluate.</param>
    /// <returns>Count of selected password items.</returns>
    private static int CountSelectedRecursive(IEnumerable<PangoExplorerItem> items)
    {
        int count = 0;
        foreach (var item in items)
        {
            if (item.IsSelected && !item.IsFolder) count++;
            if (item.Children?.Any() == true) count += CountSelectedRecursive(item.Children);
        }
        return count;
    }

    /// <summary>
    /// Resets the entire view to initial state asynchronously.
    /// </summary>
    private async Task ResetViewAsync()
    {
        Log.Logger?.Debug("ResetViewAsync started");

        await OnNavigateToOptionAsync(0);
        _decryptedPasswordCache = string.Empty;
        ImportItems.Clear();
        ImportFilePath = string.Empty;

        var passwords = await LoadPasswordsAsync();
        DisplayPasswordsInTree(passwords);

        Log.Logger?.Information("ResetViewAsync completed");
    }

    /// <summary>
    /// Opens password dialog to read and decrypt import file (Step 1 of import flow).
    /// </summary>
    private async void OnReadImportFileAsync()
    {
        Log.Logger?.Information("OnReadImportFileAsync: opening import dialog for {FilePath}", ImportFilePath);
        await _dialogService.ShowDataImportDialogAsync(new ImportDataParameters(ImportFilePath));
    }

    /// <summary>
    /// Handles import preview after successful password decryption (Step 2 of import flow).
    /// </summary>
    /// <param name="parameters">Import parameters containing decrypted content and password.</param>
    public async Task HandleImportPreviewAsync(ImportDataParameters parameters)
    {
        Log.Logger?.Debug("HandleImportPreviewAsync called");

        if (parameters is ImportDataParametersWithPassword p)
        {
            ImportFilePath = p.FilePath;
            _decryptedPasswordCache = p.Password;

            var tree = await TreeBuilder.BuildTreeAsync([.. p.PreLoadedContent]);
            ImportItems = tree;

            Log.Logger?.Information("Import preview built with {Count} items, navigating to selection tab", tree.Count);
            await OnNavigateToOptionAsync(3);
        }
        else
        {
            Log.Logger?.Warning("HandleImportPreviewAsync: parameters type mismatch");
        }
    }

    /// <summary>
    /// Executes the import operation with selected items (Step 3 of import flow).
    /// </summary>
    private async void OnFinalizeImportAsync()
    {
        Log.Logger?.Information("OnFinalizeImportAsync: starting import process");
        IsBusy = true;

        try
        {
            var selectedIds = ImportItems.FindItems(x => x.IsSelected).Select(x => x.Id).ToList();
            Log.Logger?.Debug("Selected {Count} items for import", selectedIds.Count);

            bool importToFolder = false;
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("ImportDestination", out object? val))
            {
                importToFolder = val switch
                {
                    int i => i == 1,
                    bool b => b,
                    _ => false
                };
            }
            Log.Logger?.Debug("Import destination: {Destination}", importToFolder ? "folder" : "root");

            var encoding = new EncodingOptions(_decryptedPasswordCache, "");
            var options = new Persistence.File.ImportOptions(encoding);
            var cmd = new ImportDataCommand(ImportFilePath, options, selectedIds, importToFolder);
            var result = await _sender.Send(cmd);

            if (result.IsError)
            {
                Log.Logger?.Error("Import failed: {Error}", result.FirstError.Description);
                WeakReferenceMessenger.Default.Send(
                    new InAppNotificationMessage(result.FirstError.Description, AppNotificationType.Error));
            }
            else
            {
                Log.Logger?.Information("Import completed successfully");
                WeakReferenceMessenger.Default.Send(
                    new InAppNotificationMessage(ViewResourceLoader.GetString("ImportCompleted_Message")));
                WeakReferenceMessenger.Default.Send(new ImportCompletedMessage(result.Value));
                WeakReferenceMessenger.Default.Send(new NavigationRequestedMessage(
                    new NavigationParameters(AppView.PasswordsIndex, AppView.ExportImport)));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Import finalization failed");
            WeakReferenceMessenger.Default.Send(
                new InAppNotificationMessage("Import failed", AppNotificationType.Error));
        }
        finally
        {
            IsBusy = false;
            Log.Logger?.Debug("OnFinalizeImportAsync completed");
        }
    }

    /// <summary>
    /// Opens export dialog with prepared content asynchronously.
    /// </summary>
    private async void OnExportAsync()
    {
        Log.Logger?.Information("OnExportAsync: opening export dialog");
        await _dialogService.ShowDataExportDialogAsync(new ExportDataParameters(PrepareContent()));
    }

    /// <summary>
    /// Prepares list of selected items for export operation.
    /// </summary>
    /// <returns>List of ExportItem objects representing selected passwords.</returns>
    private List<ExportItem> PrepareContent()
    {
        Log.Logger?.Debug("PrepareContent: collecting selected items for export");
        var result = new List<ExportItem>();

        void CollectFiles(IEnumerable<PangoExplorerItem> items)
        {
            foreach (var item in items)
            {
                if (item.IsSelected)
                {
                    if (!item.IsFolder)
                    {
                        result.Add(new ExportItem(Domain.Enums.ContentType.Passwords, item.Id));
                        Log.Logger?.Debug("Added password {ItemName} to export list", item.Name);
                    }
                    else
                    {
                        CollectFiles(item.Children);
                    }
                }
                else
                {
                    CollectFiles(item.Children);
                }
            }
        }

        CollectFiles(Passwords);
        Log.Logger?.Debug("PrepareContent completed: {Count} items prepared", result.Count);
        return result;
    }

    /// <summary>
    /// Recursively finds all items matching the predicate in the tree structure.
    /// </summary>
    /// <param name="sourceList">Source collection to search.</param>
    /// <param name="predicate">Filter predicate function.</param>
    /// <param name="result">List to populate with matching items.</param>
    private static void FindAllRecursive(
        IEnumerable<PangoExplorerItem> sourceList,
        Func<PangoExplorerItem, bool> predicate,
        List<PangoExplorerItem> result)
    {
        if (sourceList == null) return;

        foreach (var item in sourceList)
        {
            if (predicate(item)) result.Add(item);
            if (item.Children?.Count > 0)
            {
                FindAllRecursive(item.Children, predicate, result);
            }
        }
    }

    /// <summary>
    /// Handles navigation between export/import view tabs asynchronously.
    /// </summary>
    /// <param name="option">Target tab index: 0=general, 1=export, 2=import file, 3=import selection.</param>
    private async Task OnNavigateToOptionAsync(int option)
    {
        Log.Logger?.Debug("OnNavigateToOptionAsync: navigating to option {Option}", option);
        SelectedOption = option;

        switch (option)
        {
            case 0:
                Log.Logger?.Debug("Navigated to general tab");
                break;
            case 1:
                Log.Logger?.Debug("Navigated to export tab: loading passwords");
                await OnNavigatedToExportAsync();
                break;
            case 2:
                Log.Logger?.Debug("Navigated to import file tab");
                break;
            case 3:
                Log.Logger?.Debug("Navigated to import selection tab");
                break;
            default:
                Log.Logger?.Warning("Unknown navigation option: {Option}", option);
                break;
        }
    }

    /// <summary>
    /// Loads passwords from application layer and maps to tree items asynchronously.
    /// </summary>
    /// <returns>Enumerable of mapped PangoExplorerItem objects.</returns>
    private async Task<IEnumerable<PangoExplorerItem>> LoadPasswordsAsync()
    {
        Log.Logger?.Debug("LoadPasswordsAsync: fetching from application layer");

        var queryResult = await _sender.Send<ErrorOr<IEnumerable<PangoPasswordListItemDto>>>(
            new UserPasswordsQuery());

        if (queryResult.IsError)
        {
            Log.Logger?.Warning("LoadPasswordsAsync failed: {Error}", queryResult.FirstError);
            return [];
        }

        return await Task.Run(() =>
        {
            var items = queryResult.Value.Adapt<IEnumerable<PangoExplorerItem>>().ToList();
            foreach (var item in items)
            {
                var originalDto = queryResult.Value.FirstOrDefault(x => x.Id == item.Id);
                if (originalDto != null)
                {
                    item.Type = originalDto.IsCatalog
                        ? PangoExplorerItem.ExplorerItemType.Folder
                        : PangoExplorerItem.ExplorerItemType.File;
                    item.CatalogPath = originalDto.CatalogPath ?? string.Empty;
                }
            }
            Log.Logger?.Debug("LoadPasswordsAsync: mapped {Count} items", items.Count);
            return items;
        });
    }

    /// <summary>
    /// Displays passwords in a tree view structure asynchronously.
    /// </summary>
    /// <param name="passwords">Enumerable of password items to display.</param>
    private void DisplayPasswordsInTree(IEnumerable<PangoExplorerItem> passwords)
    {
        Log.Logger?.Debug("DisplayPasswordsInTree: building tree with {Count} items", passwords.Count());

        Task.Run(() =>
        {
            var rootItems = new List<PangoExplorerItem>();
            var folderMap = new Dictionary<string, PangoExplorerItem>(StringComparer.OrdinalIgnoreCase);

            foreach (var pwd in passwords.Where(p => p.IsFolder))
            {
                string fullPath = string.IsNullOrEmpty(pwd.CatalogPath)
                    ? pwd.Name
                    : $"{pwd.CatalogPath}{AppConstants.CatalogDelimeter}{pwd.Name}";
                pwd.Children = [];
                folderMap[fullPath] = pwd;
            }

            foreach (var pwd in passwords)
            {
                if (string.IsNullOrEmpty(pwd.CatalogPath))
                {
                    rootItems.Add(pwd);
                }
                else if (folderMap.TryGetValue(pwd.CatalogPath, out var parentFolder))
                {
                    pwd.Parent = parentFolder;
                    parentFolder.Children.Add(pwd);
                }
                else
                {
                    rootItems.Add(pwd);
                }
            }

            SortRecursive(rootItems);

            App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                Passwords = [.. rootItems];
                Log.Logger?.Debug("DisplayPasswordsInTree: UI updated with {Count} root items", rootItems.Count);
            });
        });
    }

    /// <summary>
    /// Recursively sorts tree items: folders first, then alphabetically by name.
    /// </summary>
    /// <param name="items">List of items at current level to sort.</param>
    private static void SortRecursive(List<PangoExplorerItem> items)
    {
        items.Sort((a, b) =>
        {
            if (a.Type != b.Type) return a.Type == PangoExplorerItem.ExplorerItemType.Folder ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var item in items.Where(i => i.IsFolder))
        {
            var childList = item.Children.ToList();
            SortRecursive(childList);
            item.Children = [.. childList];
        }
    }

    /// <summary>
    /// Loads and displays passwords when navigating to export tab asynchronously.
    /// </summary>
    private async Task OnNavigatedToExportAsync()
    {
        Log.Logger?.Debug("OnNavigatedToExportAsync: loading passwords for export view");
        var passwords = await LoadPasswordsAsync();
        DisplayPasswordsInTree(passwords);
    }

    #endregion
}
