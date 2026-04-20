using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.DeletePassword;
using Pango.Application.UseCases.Password.Commands.MovePasswordsToCatalog;
using Pango.Application.UseCases.Password.Commands.ToggleStar;
using Pango.Application.UseCases.Password.Queries.FindUserPassword;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using Pango.Desktop.Uwp.Core;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Extensions;
using Pango.Desktop.Uwp.Dialogs;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Models.Parameters;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Serilog;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Pango.Desktop.Uwp.ViewModels;

/// <summary>
/// View model for managing passwords and catalogs in a tree view structure.
/// </summary>
[AppView(AppView.PasswordsIndex)]
public sealed partial class PasswordsViewModel : ViewModelBase
{
    private readonly ISender _sender;
    private readonly IDialogService _dialogService;
    private readonly IUserContextProvider _userContextProvider;
    private static readonly SemaphoreSlim _cacheFileLock = new(1, 1);

    private bool _hasPasswords;
    private PangoExplorerItem? _selectedItem;
    private ObservableCollection<PangoExplorerItem> _originalList = [];
    private ObservableCollection<PangoExplorerItem> _passwords = [];
    private CancellationTokenSource? _searchCts;
    private string _searchText = string.Empty;
    private bool _isLoaded;
    private bool _needsRefresh;
    private bool _showOnlyStarred;
    private bool _isReloading;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordsViewModel"/> class.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands and queries.</param>
    /// <param name="dialogService">Service for showing modal dialogs.</param>
    /// <param name="userContextProvider">Service for accessing current user context.</param>
    /// <param name="logger">Logger instance for this view model.</param>
    public PasswordsViewModel(
        ISender sender,
        IDialogService dialogService,
        IUserContextProvider userContextProvider,
        ILogger<PasswordsViewModel> logger)
        : base(logger)
    {
        _sender = sender;
        _dialogService = dialogService;
        _userContextProvider = userContextProvider;

        SearchCommand = new RelayCommand<string>(OnFilterAsync);
        CreatePasswordCommand = new RelayCommand(OnCreatePassword);
        CreateCatalogCommand = new RelayCommand(OnCreateCatalogAsync);
        DeleteCommand = new RelayCommand<PangoExplorerItem>(OnDeleteAsync, CanDelete);
        EditPasswordCommand = new RelayCommand<PangoExplorerItem>(OnEditPasswordAsync, CanEdit);
        CopyPasswordToClipboardCommand = new RelayCommand<PangoExplorerItem>(OnCopyPasswordToClipboardAsync);
        SeePasswordCommand = new RelayCommand<PangoExplorerItem>(OnSeePasswordCommandAsync);
        UpdateListCommand = new RelayCommand(OnUpdateListAsync);
        ToggleStarCommand = new RelayCommand<PangoExplorerItem>(OnToggleStarAsync);

        App.Current.LoginSucceeded += Current_LoginSucceededAsync;

        Log.Logger?.Debug("PasswordsViewModel initialized for user: {UserName}", _userContextProvider.GetUserName());
    }

    #region Commands

    /// <summary>
    /// Command for deleting a password or catalog item.
    /// </summary>
    public RelayCommand<PangoExplorerItem> DeleteCommand { get; }

    /// <summary>
    /// Command for creating a new catalog folder.
    /// </summary>
    public RelayCommand CreateCatalogCommand { get; }

    /// <summary>
    /// Command for creating a new password entry.
    /// </summary>
    public RelayCommand CreatePasswordCommand { get; }

    /// <summary>
    /// Command for filtering passwords by search text.
    /// </summary>
    public RelayCommand<string> SearchCommand { get; }

    /// <summary>
    /// Command for editing an existing password or catalog.
    /// </summary>
    public RelayCommand<PangoExplorerItem> EditPasswordCommand { get; }

    /// <summary>
    /// Command for copying password value to clipboard.
    /// </summary>
    public RelayCommand<PangoExplorerItem> CopyPasswordToClipboardCommand { get; }

    /// <summary>
    /// Command for viewing password details in a modal dialog.
    /// </summary>
    public RelayCommand<PangoExplorerItem> SeePasswordCommand { get; }

    /// <summary>
    /// Command for refreshing the passwords list.
    /// </summary>
    public RelayCommand UpdateListCommand { get; }

    /// <summary>
    /// Command for toggling the starred status of a password.
    /// </summary>
    public RelayCommand<PangoExplorerItem> ToggleStarCommand { get; }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the observable collection of password tree items.
    /// </summary>
    public ObservableCollection<PangoExplorerItem> Passwords
    {
        get => _passwords;
        private set => SetProperty(ref _passwords, value);
    }

    /// <summary>
    /// Gets or sets the currently selected tree item.
    /// </summary>
    public PangoExplorerItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                EditPasswordCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();

                Log.Logger?.Debug("SelectedItem changed to: {ItemName}", value?.Name ?? "null");
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether any passwords exist.
    /// </summary>
    public bool HasPasswords
    {
        get => _hasPasswords;
        set
        {
            if (SetProperty(ref _hasPasswords, value))
            {
                OnPropertyChanged(nameof(ShowInitialScreen));
                Log.Logger?.Debug("HasPasswords changed to: {Value}", value);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether to show the initial empty state screen.
    /// </summary>
    public bool ShowInitialScreen => !HasPasswords && _originalList.Count == 0;

    /// <summary>
    /// Gets or sets the current search filter text.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                Log.Logger?.Debug("SearchText changed to: '{Text}'", value);
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show only starred passwords.
    /// </summary>
    public bool ShowOnlyStarred
    {
        get => _showOnlyStarred;
        set
        {
            if (SetProperty(ref _showOnlyStarred, value))
            {
                Log.Logger?.Debug("ShowOnlyStarred changed to: {Value}", value);
                ApplyFilter();
            }
        }
    }

    #endregion

    #region Overrides

    /// <summary>
    /// Registers message subscriptions for password and import events.
    /// </summary>
    protected override void RegisterMessages()
    {
        base.RegisterMessages();
        WeakReferenceMessenger.Default.Register<PasswordCreatedMessage>(this, OnPasswordCreated);
        WeakReferenceMessenger.Default.Register<PasswordUpdatedMessage>(this, OnPasswordUpdatedAsync);
        WeakReferenceMessenger.Default.Register<ImportCompletedMessage>(this, OnImportCompleted);
        Log.Logger?.Debug("Message subscriptions registered");
    }

    /// <summary>
    /// Called when the view is navigated to: resets view and handles navigation parameters.
    /// </summary>
    /// <param name="parameter">Optional navigation parameters.</param>
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        Log.Logger?.Debug("PasswordsViewModel navigated to");

        await base.OnNavigatedToAsync(parameter);

        var navParams = parameter as NavigationParameters;

        if (navParams?.SourceView == AppView.ExportImport)
        {
            _needsRefresh = true;
        }

        if (!_isLoaded || _needsRefresh)
        {
            Log.Logger?.Debug("Resetting view due to first load or refresh flag");
            await ResetViewAsync();
            _isLoaded = true;
            _needsRefresh = false;
        }

        if (navParams?.Parameter is EditPasswordParameters editParams)
        {
            AppView originalSource = navParams.SourceView;
            Log.Logger?.Debug("Navigating to EditPassword with parameters");

            WeakReferenceMessenger.Default.Send(new SwitchPasswordTabMessage(1));
            WeakReferenceMessenger.Default.Send(
                new NavigationRequestedMessage(
                    new NavigationParameters(AppView.EditPassword, originalSource, editParams)));
        }
        else
        {
            Log.Logger?.Debug("Switching to PasswordsIndex tab");
            WeakReferenceMessenger.Default.Send(new SwitchPasswordTabMessage(0));
        }
    }

    #endregion

    #region Event & Command Handlers

    /// <summary>
    /// Sets the dirty flag to force tree refresh when import completes.
    /// </summary>
    /// <param name="recipient">Message recipient instance.</param>
    /// <param name="message">Import completed message.</param>
    private void OnImportCompleted(object recipient, ImportCompletedMessage message)
    {
        Log.Logger?.Information("ImportCompletedMessage received: marking view for refresh");
        _needsRefresh = true;
        App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(async () => await ResetViewAsync());
    }

    /// <summary>
    /// Handles successful login: resets the password view asynchronously.
    /// </summary>
    /// <param name="userId">Authenticated user identifier.</param>
    private async void Current_LoginSucceededAsync(string userId)
    {
        Log.Logger?.Information("Login succeeded for user: {UserId}, resetting password view", userId);
        await ResetViewAsync();
    }

    /// <summary>
    /// Handles see password command: shows password details dialog asynchronously.
    /// </summary>
    /// <param name="item">Selected password item or null.</param>
    private async void OnSeePasswordCommandAsync(PangoExplorerItem? item)
    {
        if (item != null)
        {
            Log.Logger?.Debug("Showing password details for: {ItemName}", item.Name);
            await ShowPasswordDetailsAsync(item);
        }
    }

    /// <summary>
    /// Handles password created message: refreshes the view asynchronously.
    /// </summary>
    /// <param name="recipient">Message recipient instance.</param>
    /// <param name="message">Password created message.</param>
    private void OnPasswordCreated(object recipient, PasswordCreatedMessage message)
    {
        Log.Logger?.Information("PasswordCreatedMessage received: refreshing view");
        App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(async () => await ResetViewAsync());
    }

    /// <summary>
    /// Handles password updated message: refreshes the view asynchronously.
    /// </summary>
    /// <param name="recipient">Message recipient instance.</param>
    /// <param name="message">Password updated message.</param>
    private void OnPasswordUpdatedAsync(object recipient, PasswordUpdatedMessage message)
    {
        Log.Logger?.Information("PasswordUpdatedMessage received: refreshing view");
        App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(async () => await ResetViewAsync());
    }

    /// <summary>
    /// Copies password value to clipboard asynchronously and shows confirmation notification.
    /// </summary>
    /// <param name="dto">Password item to copy or null.</param>
    private async void OnCopyPasswordToClipboardAsync(PangoExplorerItem? dto)
    {
        if (dto is null)
        {
            Log.Logger?.Warning("CopyPasswordToClipboardAsync called with null item");
            return;
        }

        Log.Logger?.Debug("Copying password to clipboard for: {ItemName}", dto.Name);

        var passwordResult = await _sender.Send(new FindUserPasswordQuery(dto.Id));
        if (!passwordResult.IsError)
        {
            var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
            dataPackage.SetText(passwordResult.Value.Value?.ToString() ?? string.Empty);
            Clipboard.SetContent(dataPackage);

            WeakReferenceMessenger.Default.Send(
                new InAppNotificationMessage(ViewResourceLoader.GetString("PasswordCopiedToClipboard")));

            Log.Logger?.Information("Password copied to clipboard: {ItemName}", dto.Name);
        }
        else
        {
            Log.Logger?.Warning("Failed to fetch password for clipboard: {ItemName}", dto.Name);
        }
    }

    /// <summary>
    /// Deletes a password or catalog item asynchronously with confirmation.
    /// </summary>
    /// <param name="dto">Item to delete or null.</param>
    private async void OnDeleteAsync(PangoExplorerItem? dto)
    {
        dto ??= SelectedItem;

        if (dto is null)
        {
            Log.Logger?.Warning("DeleteAsync called with null item");
            return;
        }

        Log.Logger?.Information("DeleteAsync requested for: {ItemName} ({ItemType})", dto.Name, dto.Type);

        string confirmationTitle = ViewResourceLoader.GetString("Confirm_PasswordDeletion");
        string confirmationDescription = dto.Type == PangoExplorerItem.ExplorerItemType.Folder
            ? string.Format(ViewResourceLoader.GetString("RemoveCatalog_Message"), dto.Name, dto.Children?.Count ?? 0)
            : ViewResourceLoader.GetString("Confirm_PasswordDeletionDetails");

        string completionMessage = dto.Type == PangoExplorerItem.ExplorerItemType.Folder
            ? string.Format(ViewResourceLoader.GetString("CatalogDeleted_FormattedMessage"), dto.Name)
            : string.Format(ViewResourceLoader.GetString("PasswordDeleted_Format"), dto.Name);

        bool deletionConfirmed = await _dialogService.ConfirmAsync(confirmationTitle, confirmationDescription);
        if (!deletionConfirmed)
        {
            Log.Logger?.Debug("Deletion cancelled by user confirmation");
            return;
        }

        var result = await _sender.Send(new DeletePasswordCommand(dto.Id));

        if (result.IsError)
        {
            Log.Logger?.Error("Deleting {ItemType} \"{ItemName}\" failed: {Error}",
                dto.Type == PangoExplorerItem.ExplorerItemType.File ? "password" : "catalog",
                dto.Name,
                result.FirstError);

            WeakReferenceMessenger.Default.Send(
                new InAppNotificationMessage(completionMessage, AppNotificationType.Error));
        }
        else
        {
            Log.Logger?.Information("{ItemType} \"{ItemName}\" successfully deleted",
                dto.Type == PangoExplorerItem.ExplorerItemType.File ? "Password" : "Catalog",
                dto.Name);

            WeakReferenceMessenger.Default.Send(
                new InAppNotificationMessage(completionMessage, AppNotificationType.Success));
            await ResetViewAsync();
        }
    }

    /// <summary>
    /// Opens edit dialog for password or catalog asynchronously.
    /// </summary>
    /// <param name="selected">Selected item to edit or null.</param>
    private async void OnEditPasswordAsync(PangoExplorerItem? selected)
    {
        selected ??= SelectedItem;

        if (selected is null)
        {
            Log.Logger?.Warning("EditPasswordAsync called with null item");
            return;
        }

        Log.Logger?.Debug("EditPasswordAsync requested for: {ItemName} ({ItemType})", selected.Name, selected.Type);

        if (selected.Type == PangoExplorerItem.ExplorerItemType.Folder)
        {
            await _dialogService.ShowNewCatalogDialogAsync(
                new EditCatalogParameters(
                    GetAvailableCatalogs(),
                    GetPathToSelectedFolder(),
                    selected,
                    (selected?.Parent?.Children ?? Passwords)
                        ?.Where(c => c.Type == PangoExplorerItem.ExplorerItemType.Folder)
                        .Select(c => c.Name)
                        .ToList() ?? []));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(
                new NavigationRequestedMessage(
                    new NavigationParameters(
                        AppView.EditPassword,
                        AppView.PasswordsIndex,
                        new EditPasswordParameters(false, null, selected?.Id, GetAvailableCatalogs()))));
        }
    }

    /// <summary>
    /// Navigates to create new password view via messenger.
    /// </summary>
    private void OnCreatePassword()
    {
        Log.Logger?.Debug("CreatePassword command executed");
        WeakReferenceMessenger.Default.Send(
            new NavigationRequestedMessage(
                new NavigationParameters(
                    AppView.EditPassword,
                    AppView.PasswordsIndex,
                    new EditPasswordParameters(true, GetPathToSelectedFolder(), null, GetAvailableCatalogs()))));
    }

    /// <summary>
    /// Opens create new catalog dialog asynchronously.
    /// </summary>
    private async void OnCreateCatalogAsync()
    {
        Log.Logger?.Debug("CreateCatalog command executed");
        await _dialogService.ShowNewCatalogDialogAsync(
            new EditCatalogParameters(
                GetAvailableCatalogs(),
                GetPathToSelectedFolder(),
                null,
                (SelectedItem?.Children ?? Passwords)
                    ?.Where(c => c.Type == PangoExplorerItem.ExplorerItemType.Folder)
                    .Select(c => c.Name)
                    .ToList() ?? []));
    }

    /// <summary>
    /// Refreshes the password list asynchronously.
    /// </summary>
    private async void OnUpdateListAsync()
    {
        Log.Logger?.Information("UpdateList command executed: resetting view");
        await ResetViewAsync();
    }

    /// <summary>
    /// Applies current search and starred filters to the password tree.
    /// </summary>
    private void ApplyFilter()
    {
        Log.Logger?.Debug("ApplyFilter called: ShowOnlyStarred={ShowOnlyStarred}", ShowOnlyStarred);

        var snapshot = Passwords.ToList();

        Task.Run(() =>
        {
            var visibilityMap = new Dictionary<Guid, bool>();
            bool hasVisible = false;

            foreach (var item in snapshot)
            {
                if (CalculateStarredVisibility(item, visibilityMap))
                    hasVisible = true;
            }

            App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(async () =>
            {
                await ApplyVisibilityMapAsync(Passwords, visibilityMap);
                HasPasswords = hasVisible;
            });
        });
    }

    private bool CalculateStarredVisibility(PangoExplorerItem node, Dictionary<Guid, bool> visibilityMap)
    {
        if (node.Type == PangoExplorerItem.ExplorerItemType.File)
        {
            bool isVis = !_showOnlyStarred || node.IsStar;
            visibilityMap[node.Id] = isVis;
            return isVis;
        }

        bool hasVisibleChild = false;
        foreach (var child in node.Children)
        {
            if (CalculateStarredVisibility(child, visibilityMap))
                hasVisibleChild = true;
        }

        bool isFolderVis = !_showOnlyStarred || hasVisibleChild;
        visibilityMap[node.Id] = isFolderVis;
        return isFolderVis;
    }

    /// <summary>
    /// Recursively calculates visibility for tree items based on filter criteria.
    /// </summary>
    /// <param name="item">Current tree item to evaluate.</param>
    /// <returns>True if item or any descendant should be visible.</returns>
    private bool ApplyFilterRecursive(PangoExplorerItem item)
    {
        if (item.Type == PangoExplorerItem.ExplorerItemType.File)
        {
            item.IsVisible = !ShowOnlyStarred || item.IsStar;
            return item.IsVisible;
        }

        bool hasVisibleChild = false;
        foreach (var child in item.Children)
        {
            if (ApplyFilterRecursive(child))
                hasVisibleChild = true;
        }

        item.IsVisible = !ShowOnlyStarred || hasVisibleChild;
        return item.IsVisible;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Opens the modal dialog showing the details of the selected password.
    /// </summary>
    /// <param name="selectedPassword">Password item to display details for.</param>
    public async Task ShowPasswordDetailsAsync(PangoExplorerItem selectedPassword)
    {
        if (selectedPassword.IsFolder)
        {
            OnEditPasswordAsync(selectedPassword);
            return;
        }

        Log.Logger?.Debug("Showing password details dialog for: {ItemName}", selectedPassword.Name);
        await _dialogService.ShowPasswordDetailsAsync(
            new PasswordDetailsParameters(selectedPassword.Id, GetAvailableCatalogs()));
    }

    /// <summary>
    /// Commits password movement to new parent and persists changes asynchronously.
    /// </summary>
    /// <param name="movedItem">Item that was moved in the tree.</param>
    /// <param name="newParent">New parent item or null for root.</param>
    public async Task CommitPasswordMovementAsync(PangoExplorerItem movedItem, PangoExplorerItem? newParent)
    {
        Log.Logger?.Information("CommitPasswordMovementAsync: moving {ItemName} to {ParentName}",
            movedItem.Name,
            newParent?.Name ?? "root");

        movedItem.Parent = newParent;
        movedItem.RecalculateCatalogPath();

        Dictionary<Guid, string> passwordItemsToUpdate = BuildPasswordAndCatalogPathPairs([movedItem]);

        if (passwordItemsToUpdate.Count > 0)
        {
            await _sender.Send(new MovePasswordsToCatalogCommand(passwordItemsToUpdate));
            Log.Logger?.Debug("MovePasswordsToCatalogCommand sent for {Count} items", passwordItemsToUpdate.Count);
        }

        await ResetViewAsync();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Toggles the starred status of a password asynchronously.
    /// </summary>
    /// <param name="item">Password item to toggle or null.</param>
    private async void OnToggleStarAsync(PangoExplorerItem? item)
    {
        if (item is null || item.IsFolder)
        {
            Log.Logger?.Warning("ToggleStarAsync called with null or folder item");
            return;
        }

        Log.Logger?.Debug("ToggleStarAsync for: {ItemName}", item.Name);

        bool newStarState = !item.IsStar;
        item.IsStar = newStarState;

        var result = await _sender.Send(new TogglePasswordStarCommand(item.Id, newStarState));

        if (result.IsError)
        {
            item.IsStar = !newStarState;
            Log.Logger?.Error("Failed to toggle star for password {Name}: {Error}", item.Name, result.FirstError);
        }
        else
        {
            Log.Logger?.Information("Star toggled for password {Name}: {NewState}", item.Name, newStarState);
            if (ShowOnlyStarred && !newStarState)
            {
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Handles the command to filter passwords content safely on the UI thread asynchronously.
    /// </summary>
    /// <param name="searchText">Search text to filter by or null.</param>
    private void OnFilterAsync(string? searchText)
    {
        Log.Logger?.Debug("OnFilterAsync called with text: '{Text}'", searchText ?? string.Empty);

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Func<PangoExplorerItem, bool> searchPredicate = string.IsNullOrWhiteSpace(searchText)
            ? _ => true
            : i => i.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);

        var snapshot = Passwords.ToList();

        Task.Run(() =>
        {
            var visibilityMap = new Dictionary<Guid, bool>();
            bool hasVisible = false;

            foreach (PangoExplorerItem password in snapshot)
            {
                if (token.IsCancellationRequested) return;
                if (CalculateVisibility(password, searchPredicate, visibilityMap))
                    hasVisible = true;
            }

            if (token.IsCancellationRequested) return;

            App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(async () =>
            {
                await ApplyVisibilityMapAsync(Passwords, visibilityMap);
                HasPasswords = hasVisible;
                Log.Logger?.Debug("Filter applied: HasPasswords={HasPasswords}", hasVisible);
            });
        }, token);
    }

    /// <summary>
    /// Recursively calculates visibility without touching UI properties.
    /// </summary>
    /// <param name="node">Current tree node to evaluate.</param>
    /// <param name="searchPredicate">Predicate function for text matching.</param>
    /// <param name="visibilityMap">Dictionary to store pre-calculated visibility results.</param>
    /// <returns>True if node or any descendant matches the criteria.</returns>
    private static bool CalculateVisibility(
        PangoExplorerItem node,
        Func<PangoExplorerItem, bool> searchPredicate,
        Dictionary<Guid, bool> visibilityMap)
    {
        if (node is null) return false;

        if (node.Type == PangoExplorerItem.ExplorerItemType.File)
        {
            bool isVis = searchPredicate(node);
            visibilityMap[node.Id] = isVis;
            return isVis;
        }

        bool hasVisibleItems = false;
        if (node.Children.Any())
        {
            foreach (PangoExplorerItem item in node.Children)
            {
                if (CalculateVisibility(item, searchPredicate, visibilityMap))
                    hasVisibleItems = true;
            }
        }

        bool isFolderVis = hasVisibleItems || searchPredicate(node);
        visibilityMap[node.Id] = isFolderVis;
        return isFolderVis;
    }

    /// <summary>
    /// Applies pre-calculated visibility to the Observable models safely on the UI thread.
    /// </summary>
    /// <param name="nodes">Collection of tree nodes to update.</param>
    /// <param name="visibilityMap">Dictionary containing pre-calculated visibility values.</param>
    private static async Task ApplyVisibilityMapAsync(
            IEnumerable<PangoExplorerItem> nodes,
            Dictionary<Guid, bool> visibilityMap)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            if (visibilityMap.TryGetValue(node.Id, out bool isVis) && node.IsVisible != isVis)
            {
                node.IsVisible = isVis;
                count++;
            }

            if (node.Children?.Count > 0)
            {
                await ApplyVisibilityMapAsync(node.Children, visibilityMap);
            }

            if (count > 30)
            {
                count = 0;
                await Task.Delay(1);
            }
        }
    }

    /// <summary>
    /// Determines if an item can be deleted based on null check.
    /// </summary>
    /// <param name="item">Item to evaluate or null.</param>
    /// <returns>True if item is not null.</returns>
    private bool CanDelete(PangoExplorerItem? item) => item != null || SelectedItem != null;

    /// <summary>
    /// Determines if an item can be edited based on null check.
    /// </summary>
    /// <param name="item">Item to evaluate or null.</param>
    /// <returns>True if item is not null.</returns>
    private bool CanEdit(PangoExplorerItem? item) => item != null || SelectedItem != null;

    /// <summary>
    /// Resets the view to initial state and reloads passwords asynchronously.
    /// </summary>
    private async Task ResetViewAsync()
    {
        if (_isReloading) return;
        _isReloading = true;

        try
        {
            Log.Logger?.Debug("ResetViewAsync started");

            SelectedItem = null;
            SearchText = string.Empty;
            var expandedPaths = new HashSet<string>();

            foreach (var item in Passwords)
            {
                SaveExpandedState(item, expandedPaths);
            }

            IEnumerable<PangoExplorerItem> passwords = await LoadPasswordsAsync();
            SetOriginalList(passwords);

            await DisplayPasswordsInTreeAsync(passwords, expandedPaths);
            Log.Logger?.Information("ResetViewAsync completed: loaded {Count} passwords", passwords.Count());
        }
        finally
        {
            _isReloading = false;
        }
    }

    /// <summary>
    /// Saves expanded state of folders for restoration after refresh.
    /// </summary>
    /// <param name="item">Current tree item to process.</param>
    /// <param name="states">HashSet to collect expanded folder paths.</param>
    private static void SaveExpandedState(PangoExplorerItem item, HashSet<string> states)
    {
        if (item.IsFolder && item.IsExpanded)
        {
            string fullPath = string.IsNullOrEmpty(item.CatalogPath)
                ? item.Name
                : $"{item.CatalogPath}{AppConstants.CatalogDelimeter}{item.Name}";
            states.Add(fullPath);
        }
        if (item.Children != null)
        {
            foreach (var child in item.Children)
                SaveExpandedState(child, states);
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
            var items = queryResult.Value
                .Select(dto =>
                {
                    var item = new PangoExplorerItem(
                        dto.Id,
                        dto.Name,
                        dto.IsCatalog ? PangoExplorerItem.ExplorerItemType.Folder : PangoExplorerItem.ExplorerItemType.File)
                    {
                        CatalogPath = dto.CatalogPath ?? string.Empty,
                        IsStar = dto.Star,
                        Properties = dto.Properties ?? []
                    };

                    if (dto.Properties != null)
                    {
                        item.Properties = dto.Properties;
                        if (dto.Properties.TryGetValue(PasswordProperties.ExpirationDate, out var expStr))
                        {
                            if (DateTimeOffset.TryParse(expStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expDate) ||
                                DateTimeOffset.TryParse(expStr, out expDate))
                            {
                                item.ExpirationDate = expDate;
                            }
                        }
                    }
                    return item;
                }).ToList();

            Log.Logger?.Debug("LoadPasswordsAsync: mapped {Count} items", items.Count);
            return items.OrderBy(i => i.NestingLevel).ToList();
        });
    }

    /// <summary>
    /// Caches flat representation of passwords to drive initial screen visibility.
    /// </summary>
    /// <param name="passwords">Enumerable of password items to cache.</param>
    private void SetOriginalList(IEnumerable<PangoExplorerItem> passwords)
    {
        Log.Logger?.Debug("SetOriginalList: caching {Count} items", passwords.Count());
        _originalList = [.. passwords];
    }

    /// <summary>
    /// Displays passwords in a tree view and checks expirations asynchronously.
    /// </summary>
    /// <param name="passwords">Enumerable of password items to display.</param>
    /// <param name="expandedPaths">HashSet of previously expanded folder paths.</param>
    private async Task DisplayPasswordsInTreeAsync(
        IEnumerable<PangoExplorerItem> passwords,
        HashSet<string> expandedPaths)
    {
        Log.Logger?.Debug("DisplayPasswordsInTreeAsync: building tree with {Count} items", passwords.Count());

        int warningDays = 7;
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(
                Constants.Settings.ExpirationWarningDays, out object? warningObj) && warningObj != null)
        {
            warningDays = Convert.ToInt32(warningObj);
        }
        var now = DateTime.Now.Date;

        var (rootItems, userExpirations) = await Task.Run(() =>
        {
            var folderMap = new Dictionary<string, PangoExplorerItem>(StringComparer.OrdinalIgnoreCase);
            var childrenMap = new Dictionary<string, List<PangoExplorerItem>>(StringComparer.OrdinalIgnoreCase);
            List<PangoExplorerItem> rootItemsList = [];

            foreach (var pwd in passwords)
            {
                if (pwd.IsFolder)
                {
                    string fullPath = string.IsNullOrEmpty(pwd.CatalogPath)
                        ? pwd.Name
                        : $"{pwd.CatalogPath}{AppConstants.CatalogDelimeter}{pwd.Name}";
                    if (expandedPaths.Contains(fullPath)) pwd.IsExpanded = true;
                    folderMap[fullPath] = pwd;
                    childrenMap.TryAdd(fullPath, []);
                }
            }

            foreach (var pwd in passwords)
            {
                if (string.IsNullOrEmpty(pwd.CatalogPath))
                {
                    rootItemsList.Add(pwd);
                }
                else
                {
                    if (childrenMap.TryGetValue(pwd.CatalogPath, out var list))
                        list.Add(pwd);
                    else
                        rootItemsList.Add(pwd);
                }
            }

            SortAndBind(rootItemsList, childrenMap, null);

            List<PangoExplorerItem> allItems = [];
            foreach (var root in rootItemsList)
            {
                allItems.Add(root);
                allItems.AddRange(root.GetAllDescendants());
            }

            List<ExpirationCacheItem> expirationsCache = [];
            foreach (var item in allItems.Where(p => !p.IsFolder))
            {
                if (item.ExpirationDate.HasValue)
                {
                    var expDate = item.ExpirationDate.Value.LocalDateTime.Date;
                    int daysLeft = (int)(expDate - now).TotalDays;
                    string dateStr = expDate.ToString("d", CultureInfo.CurrentCulture);

                    if (daysLeft < 0)
                    {
                        item.ExpirationStatus = PasswordExpirationStatus.Expired;
                        item.ExpirationTooltip = $"Expired {-daysLeft} days ago ({dateStr})";
                    }
                    else if (daysLeft <= warningDays)
                    {
                        item.ExpirationStatus = PasswordExpirationStatus.ExpiringSoon;
                        item.ExpirationTooltip = $"Expires in {daysLeft} days ({dateStr})";
                    }
                    else
                    {
                        item.ExpirationStatus = PasswordExpirationStatus.Valid;
                        item.ExpirationTooltip = $"Expires in {daysLeft} days ({dateStr})";
                    }

                    expirationsCache.Add(new ExpirationCacheItem { Name = item.Name, Date = item.ExpirationDate.Value });
                }
                else
                {
                    item.ExpirationStatus = PasswordExpirationStatus.Valid;
                    item.ExpirationTooltip = null;
                }
            }

            foreach (var folder in allItems.Where(p => p.IsFolder).OrderByDescending(p => p.NestingLevel))
            {
                if (folder.Children.Any(c => c.ExpirationStatus == PasswordExpirationStatus.Expired))
                {
                    folder.ExpirationStatus = PasswordExpirationStatus.Expired;
                    folder.ExpirationTooltip = "Contains expired passwords";
                }
                else if (folder.Children.Any(c => c.ExpirationStatus == PasswordExpirationStatus.ExpiringSoon))
                {
                    folder.ExpirationStatus = PasswordExpirationStatus.ExpiringSoon;
                    folder.ExpirationTooltip = "Contains passwords expiring soon";
                }
                else
                {
                    folder.ExpirationStatus = PasswordExpirationStatus.Valid;
                    folder.ExpirationTooltip = null;
                }
            }

            foreach (var item in rootItemsList)
            {
                ApplyFilterRecursive(item);
            }

            return (rootItemsList, expirationsCache);
        });

        App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            var oldItems = Passwords.ToList();
            Passwords = new ObservableCollection<PangoExplorerItem>(rootItems);
            HasPasswords = Passwords.Count > 0;
            UpdateUserExpirationCache(userExpirations);

            foreach (var item in oldItems)
            {
                item.ReleaseReferences();
            }
            oldItems.Clear();

            Log.Logger?.Debug("DisplayPasswordsInTreeAsync: UI updated with {Count} root items", rootItems.Count);
        });
    }

    /// <summary>
    /// Recursively sorts the tree structure and binds parent-child relationships.
    /// </summary>
    /// <param name="items">List of items at current level to sort and bind.</param>
    /// <param name="childrenMap">Dictionary mapping folder paths to child lists.</param>
    /// <param name="parent">Parent item for current level or null for root.</param>
    private static void SortAndBind(
        List<PangoExplorerItem> items,
        Dictionary<string, List<PangoExplorerItem>> childrenMap,
        PangoExplorerItem? parent)
    {
        items.Sort((a, b) =>
        {
            if (a.Type != b.Type) return a.Type == PangoExplorerItem.ExplorerItemType.Folder ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var item in items)
        {
            item.Parent = parent;
            if (item.Type == PangoExplorerItem.ExplorerItemType.Folder)
            {
                string fullPath = string.IsNullOrEmpty(item.CatalogPath)
                    ? item.Name
                    : $"{item.CatalogPath}{AppConstants.CatalogDelimeter}{item.Name}";
                if (childrenMap.TryGetValue(fullPath, out var children))
                {
                    SortAndBind(children, childrenMap, item);
                    item.Children = [.. children];
                }
            }
        }
    }

    /// <summary>
    /// Saves the expiration dates cache to disk for the Background Notification Service asynchronously.
    /// </summary>
    /// <param name="newItems">List of expiration cache items for current user.</param>
    private void UpdateUserExpirationCache(List<ExpirationCacheItem> newItems)
    {
        Log.Logger?.Debug("UpdateUserExpirationCache: saving {Count} items to disk", newItems.Count);

        Task.Run(async () =>
        {
            await _cacheFileLock.WaitAsync();
            try
            {
                string configDir = ApplicationData.Current.LocalFolder.Path;
                string cacheFile = Path.Combine(configDir, "expiration_cache.json");
                Dictionary<string, List<ExpirationCacheItem>> cache = [];
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                if (File.Exists(cacheFile))
                {
                    try
                    {
                        string existingJson = await File.ReadAllTextAsync(cacheFile);
                        existingJson = existingJson.Trim();
                        if (existingJson.StartsWith('{') && existingJson.EndsWith('}'))
                        {
                            cache = JsonSerializer.Deserialize<Dictionary<string, List<ExpirationCacheItem>>>(existingJson, options) ?? [];
                        }
                    }
                    catch (JsonException ex)
                    {
                        Log.Logger?.Warning(ex, "Failed to parse existing expiration cache JSON");
                    }
                }

                string currentUser = _userContextProvider.GetUserName();
                if (!string.IsNullOrEmpty(currentUser))
                {
                    cache[currentUser] = newItems;
                    await File.WriteAllTextAsync(cacheFile, JsonSerializer.Serialize(cache, options));
                    WeakReferenceMessenger.Default.Send(new ExpirationCacheUpdatedMessage());
                    Log.Logger?.Information("Expiration cache saved for user: {UserName}", currentUser);
                }

                WeakReferenceMessenger.Default.Send(new ExpirationCacheUpdatedMessage());
            }
            catch (Exception ex)
            {
                Log.Logger?.Warning(ex, "Cannot save expiration cache to disk");
            }
            finally
            {
                _cacheFileLock.Release();
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves list of Id+CatalogPath of all passwords from tree structure.
    /// </summary>
    /// <param name="itemsSource">List of items in tree structure.</param>
    /// <returns>Dictionary mapping password IDs to their catalog paths.</returns>
    private static Dictionary<Guid, string> BuildPasswordAndCatalogPathPairs(IEnumerable<PangoExplorerItem> itemsSource)
    {
        Dictionary<Guid, string> result = [];
        BuildPairsRecursive(itemsSource, result);
        return result;
    }

    /// <summary>
    /// Recursively builds ID-to-path mapping for all tree items.
    /// </summary>
    /// <param name="itemsSource">Collection of items to process.</param>
    /// <param name="result">Dictionary to populate with results.</param>
    private static void BuildPairsRecursive(
        IEnumerable<PangoExplorerItem> itemsSource,
        Dictionary<Guid, string> result)
    {
        foreach (PangoExplorerItem treeItem in itemsSource)
        {
            result[treeItem.Id] = treeItem.CatalogPath;
            if (treeItem.Children?.Count > 0)
            {
                BuildPairsRecursive(treeItem.Children, result);
            }
        }
    }

    /// <summary>
    /// Returns a sorted list of all currently existing catalog paths.
    /// </summary>
    /// <returns>List of catalog path strings with empty string at index 0 for root.</returns>
    private List<string> GetAvailableCatalogs()
    {
        List<string> catalogs =
        [
            .. Passwords.FindItems(p => p.Type == PangoExplorerItem.ExplorerItemType.Folder)
                .Select(p => PasswordPathUtility.BuildCatalogPath(p.CatalogPath, p.Name))
                .OrderBy(p => p)
        ];

        catalogs.Insert(0, string.Empty);
        return catalogs;
    }

    /// <summary>
    /// Returns the currently selected folder path or empty string if none selected.
    /// </summary>
    /// <returns>Full catalog path string for selected folder or parent of selected item.</returns>
    private string GetPathToSelectedFolder() =>
        SelectedItem == null
            ? string.Empty
            : SelectedItem.Type == PangoExplorerItem.ExplorerItemType.Folder
                ? PasswordPathUtility.BuildCatalogPath(SelectedItem.CatalogPath, SelectedItem.Name)
                : SelectedItem.CatalogPath;

    #endregion
}
