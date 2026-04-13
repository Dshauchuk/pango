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
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.PasswordsIndex)]
public sealed partial class PasswordsViewModel : ViewModelBase
{
    private readonly ISender _sender;
    private readonly IDialogService _dialogService;
    private readonly IUserContextProvider _userContextProvider;
    private bool _hasPasswords;
    private PangoExplorerItem? _selectedItem;
    private ObservableCollection<PangoExplorerItem> _originalList;
    private string _searchText = string.Empty;
    private bool _isLoaded;
    private bool _needsRefresh;
    private string? _pendingGeneratedPassword;
    private bool _showOnlyStarred;

    public PasswordsViewModel(ISender sender, IDialogService dialogService, IUserContextProvider userContextProvider, ILogger<PasswordsViewModel> logger) : base(logger)
    {
        _sender = sender;
        _dialogService = dialogService;
        _userContextProvider = userContextProvider;

        _originalList = [];
        Passwords = [];
        Passwords.CollectionChanged += Passwords_CollectionChanged;

        SearchCommand = new RelayCommand<string>(OnFilterAsync);
        CreatePasswordCommand = new RelayCommand(OnCreatePassword);
        CreateCatalogCommand = new RelayCommand(OnCreateCatalogAsync);
        DeleteCommand = new RelayCommand<PangoExplorerItem>(OnDeleteAsync, CanDelete);
        EditPasswordCommand = new RelayCommand<PangoExplorerItem>(OnEditPasswordAsync, CanEdit);
        CopyPasswordToClipboardCommand = new RelayCommand<PangoExplorerItem>(OnCopyPasswordToClipboard);
        SeePasswordCommand = new RelayCommand<PangoExplorerItem>(OnSeePasswordCommand);
        UpdateListCommand = new RelayCommand(OnUpdateListAsync);
        ToggleStarCommand = new RelayCommand<PangoExplorerItem>(OnToggleStarAsync);

        App.Current.LoginSucceeded += Current_LoginSucceeded;
    }

    #region Commands

    public RelayCommand<PangoExplorerItem> DeleteCommand { get; }
    public RelayCommand CreateCatalogCommand { get; }
    public RelayCommand CreatePasswordCommand { get; }
    public RelayCommand<string> SearchCommand { get; }
    public RelayCommand<PangoExplorerItem> EditPasswordCommand { get; }
    public RelayCommand<PangoExplorerItem> CopyPasswordToClipboardCommand { get; }
    public RelayCommand<PangoExplorerItem> SeePasswordCommand { get; }
    public RelayCommand UpdateListCommand { get; }
    public RelayCommand<PangoExplorerItem> ToggleStarCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<PangoExplorerItem> Passwords { get; private set; }

    public PangoExplorerItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            OnPropertyChanged(nameof(EditPasswordCommand));
            OnPropertyChanged(nameof(DeleteCommand));
        }
    }

    public bool HasPasswords
    {
        get => _hasPasswords;
        set
        {
            SetProperty(ref _hasPasswords, value);
            OnPropertyChanged(nameof(ShowInitialScreen));
        }
    }

    public bool ShowInitialScreen => !HasPasswords && _originalList.Count == 0;

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }
    public bool ShowOnlyStarred
    {
        get => _showOnlyStarred;
        set
        {
            if (SetProperty(ref _showOnlyStarred, value))
                ApplyFilter();
        }
    }

    #endregion

    #region Overrides

    protected override void RegisterMessages()
    {
        base.RegisterMessages();
        WeakReferenceMessenger.Default.Register<PasswordCreatedMessage>(this, OnPasswordCreated);
        WeakReferenceMessenger.Default.Register<PasswordUpdatedMessage>(this, OnPasswordUpdatedAsync);
        WeakReferenceMessenger.Default.Register<CreatePasswordFromGeneratorMessage>(this, OnCreatePasswordFromGenerator);
        WeakReferenceMessenger.Default.Register<ImportCompletedMessage>(this, OnImportCompleted);
    }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        bool isJustSwitchingTabs = parameter is NavigationParameters navParams && navParams.Parameter == null;

        if (!_isLoaded || (parameter != null && !isJustSwitchingTabs) || _needsRefresh)
        {
            await ResetViewAsync();
            _isLoaded = true;
            _needsRefresh = false;
        }

        if (!string.IsNullOrEmpty(_pendingGeneratedPassword))
        {
            OpenEditForGeneratedPassword(_pendingGeneratedPassword);
            _pendingGeneratedPassword = null;
        }
    }

    #endregion

    #region Event&Command Handlers

    /// <summary>
    /// Sets the dirty flag to force tree refresh when user navigates back to this view.
    /// </summary>
    private void OnImportCompleted(object recipient, ImportCompletedMessage message)
    {
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogDebug("Import completed detected. Flagging passwords view for refresh.");
        }
        _needsRefresh = true;
    }

    private async void Current_LoginSucceeded(string userId) => await ResetViewAsync();

    private async void OnSeePasswordCommand(PangoExplorerItem? item)
    {
        if (item != null) await ShowPasswordDetailsAsync(item);
    }

    private void Passwords_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SelectedItem = null;
        HasPasswords = Passwords.Any(p => p.IsVisible);
    }

    private void OnPasswordCreated(object recipient, PasswordCreatedMessage message)
    {
        App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(async () => await ResetViewAsync());
    }

    private void OnPasswordUpdatedAsync(object recipient, PasswordUpdatedMessage message)
    {
        App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(async () => await ResetViewAsync());
    }

    private void OnCreatePasswordFromGenerator(object recipient, CreatePasswordFromGeneratorMessage message)
    {
        if (!_isLoaded)
        {
            _pendingGeneratedPassword = message.Value;
            return;
        }
        OpenEditForGeneratedPassword(message.Value);
    }

    private async void OnCopyPasswordToClipboard(PangoExplorerItem? dto)
    {
        if (dto is null) return;

        var passwordResult = await _sender.Send(new FindUserPasswordQuery(dto.Id));
        if (!passwordResult.IsError)
        {
            DataPackage dataPackage = new() { RequestedOperation = DataPackageOperation.Copy };
            dataPackage.SetText(passwordResult.Value.Value?.ToString() ?? string.Empty);
            Clipboard.SetContent(dataPackage);

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(ViewResourceLoader.GetString("PasswordCopiedToClipboard")));
        }
    }

    private async void OnDeleteAsync(PangoExplorerItem? dto)
    {
        if (dto is null) return;

        string confirmationTitle = ViewResourceLoader.GetString("Confirm_PasswordDeletion");
        string confirmationDescription = dto.Type == PangoExplorerItem.ExplorerItemType.Folder
            ? string.Format(ViewResourceLoader.GetString("RemoveCatalog_Message"), dto.Name, dto.Children?.Count ?? 0)
            : ViewResourceLoader.GetString("Confirm_PasswordDeletionDetails");

        string completionMessage = dto.Type == PangoExplorerItem.ExplorerItemType.Folder
            ? string.Format(ViewResourceLoader.GetString("CatalogDeleted_FormattedMessage"), dto.Name)
            : string.Format(ViewResourceLoader.GetString("PasswordDeleted_Format"), dto.Name);

        bool deletionConfirmed = await _dialogService.ConfirmAsync(confirmationTitle, confirmationDescription);
        if (!deletionConfirmed) return;

        var result = await _sender.Send(new DeletePasswordCommand(dto.Id));

        if (result.IsError)
        {
            if (Logger.IsEnabled(LogLevel.Error))
                Logger.LogError("Deleting {ItemType} \"{ItemName}\" failed: {Error}", dto.Type == PangoExplorerItem.ExplorerItemType.File ? "password" : "catalog", dto.Name, result.FirstError);

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(completionMessage, AppNotificationType.Error));
        }
        else
        {
            if (Logger.IsEnabled(LogLevel.Debug))
                Logger.LogDebug("{ItemType} \"{ItemName}\" successfully deleted", dto.Type == PangoExplorerItem.ExplorerItemType.File ? "Password" : "Catalog", dto.Name);

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(completionMessage, AppNotificationType.Success));
            await ResetViewAsync();
        }
    }

    private async void OnEditPasswordAsync(PangoExplorerItem? selected)
    {
        if (selected is null) return;

        if (selected.Type == PangoExplorerItem.ExplorerItemType.Folder)
        {
            await _dialogService.ShowNewCatalogDialogAsync(
                    new EditCatalogParameters(GetAvailableCatalogs(), GetPathToSelectedFolder(), selected, (selected?.Parent?.Children ?? Passwords)?.Where(c => c.Type == PangoExplorerItem.ExplorerItemType.Folder).Select(c => c.Name).ToList() ?? []));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new NavigationParameters(AppView.EditPassword, AppView.PasswordsIndex, new EditPasswordParameters(false, null, selected?.Id, GetAvailableCatalogs()))));
        }
    }

    private void OnCreatePassword() => WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new NavigationParameters(AppView.EditPassword, AppView.PasswordsIndex, new EditPasswordParameters(true, GetPathToSelectedFolder(), null, GetAvailableCatalogs()))));

    private async void OnCreateCatalogAsync() => await _dialogService.ShowNewCatalogDialogAsync(
                new EditCatalogParameters(GetAvailableCatalogs(), GetPathToSelectedFolder(), null, (SelectedItem?.Children ?? Passwords)?.Where(c => c.Type == PangoExplorerItem.ExplorerItemType.Folder).Select(c => c.Name).ToList() ?? []));

    private async void OnUpdateListAsync() => await ResetViewAsync();

    private void ApplyFilter()
    {
        foreach (var item in Passwords)
        {
            ApplyFilterRecursive(item);
        }
    }

    private bool ApplyFilterRecursive(PangoExplorerItem item)
    {
        if (item.Type == PangoExplorerItem.ExplorerItemType.File)
        {
            item.IsVisible = !ShowOnlyStarred || item.IsStar;
            return item.IsVisible;
        }

        // recursively check all the children
        bool hasVisibleChild = false;
        foreach (var child in item.Children)
        {
            if (ApplyFilterRecursive(child))
                hasVisibleChild = true;
        }

        // Folder is visible only if there are visible children in it (or filter enabled)
        item.IsVisible = !ShowOnlyStarred || hasVisibleChild;
        return item.IsVisible;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Opens the modal dialog showing the details of the selected password.
    /// </summary>
    public async Task ShowPasswordDetailsAsync(PangoExplorerItem selectedPassword)
        => await _dialogService.ShowPasswordDetailsAsync(new PasswordDetailsParameters(selectedPassword.Id, GetAvailableCatalogs()));

    #endregion

    #region Private Methods

    private async void OnToggleStarAsync(PangoExplorerItem? item)
    {
        if (item is null || item.IsFolder) return;

        bool newStarState = !item.IsStar;
        item.IsStar = newStarState;

        var result = await _sender.Send(new TogglePasswordStarCommand(item.Id, newStarState));

        if (result.IsError)
        {
            item.IsStar = !newStarState;
            if (Logger.IsEnabled(LogLevel.Error))
            {
                Logger.LogError("Failed to toggle star for password {Name}: {Error}", item.Name, result.FirstError);
            }
        }
        else
        {
            if (ShowOnlyStarred && !newStarState)
            {
                ApplyFilter();
            }
        }
    }

    /// <summary>
    /// Handles the command to filter passwords content safely on the UI thread
    /// </summary>
    /// <param name="searchText"></param>
    private void OnFilterAsync(string? searchText)
    {
        Func<PangoExplorerItem, bool> searchPredicate = string.IsNullOrWhiteSpace(searchText)
            ? (i) => true
            : (i) => i.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);

        Task.Run(() =>
        {
            var visibilityMap = new Dictionary<Guid, bool>();
            bool hasVisible = false;

            foreach (PangoExplorerItem password in Passwords)
            {
                if (CalculateVisibility(password, searchPredicate, visibilityMap))
                {
                    hasVisible = true;
                }
            }

            App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                ApplyVisibilityMap(Passwords, visibilityMap);
                HasPasswords = hasVisible;
            });
        });
    }

    /// <summary>
    /// Recursively calculates visibility without touching UI properties.
    /// </summary>
    private static bool CalculateVisibility(PangoExplorerItem node, Func<PangoExplorerItem, bool> searchPredicate, Dictionary<Guid, bool> visibilityMap)
    {
        if (node is null) return false;

        if (node.Type == PangoExplorerItem.ExplorerItemType.File)
        {
            bool isVis = searchPredicate(node);
            visibilityMap[node.Id] = isVis;
            return isVis;
        }
        else
        {
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
    }

    /// <summary>
    /// Applies pre-calculated visibility to the Observable models safely on the UI thread.
    /// </summary>
    private static void ApplyVisibilityMap(IEnumerable<PangoExplorerItem> nodes, Dictionary<Guid, bool> visibilityMap)
    {
        foreach (var node in nodes)
        {
            if (visibilityMap.TryGetValue(node.Id, out bool isVis))
            {
                node.IsVisible = isVis;
            }
            if (node.Children.Any())
            {
                ApplyVisibilityMap(node.Children, visibilityMap);
            }
        }
    }

    /// <summary>
    /// Returns true and makes the <paramref name="node"/> visible if it or any its children fits <paramref name="searchPredicate"/>, otherwise - false. 
    /// </summary>
    /// <param name="node"></param>
    /// <param name="searchPredicate"></param>
    /// <returns></returns>
    private static bool Filter(PangoExplorerItem node, Func<PangoExplorerItem, bool> searchPredicate)
    {
        if (node is null) return false;
        if (node.Type == PangoExplorerItem.ExplorerItemType.File)
        {
            return node.IsVisible = searchPredicate(node);
        }
        else
        {
            bool hasVisibleItems = false;
            if (node.Children.Any())
            {
                foreach (PangoExplorerItem item in node.Children)
                {
                    if (Filter(item, searchPredicate) && !hasVisibleItems) hasVisibleItems = true;
                }
            }
            return node.IsVisible = hasVisibleItems || searchPredicate(node);
        }
    }

    /// <summary>
    /// Returns true if <paramref name="item"/> can be deleted, otherwise - false
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private bool CanDelete(PangoExplorerItem? item) => item != null;

    /// <summary>
    /// Returns true if <paramref name="item"/> can be edited, otherwise - false
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private bool CanEdit(PangoExplorerItem? item) => item != null;

    /// <summary>
    /// Resets the view
    /// </summary>
    /// <returns></returns>
    private async Task ResetViewAsync()
    {
        SelectedItem = null;
        SearchText = string.Empty;
        var expandedPaths = new HashSet<string>();

        foreach (var item in Passwords)
        {
            SaveExpandedState(item, expandedPaths);
        }

        IEnumerable<PangoExplorerItem> passwords = await LoadPasswordsAsync();
        await DisplayPasswordsInTreeAsync(passwords, expandedPaths);
        SetOriginalList(passwords);
    }

    private static void SaveExpandedState(PangoExplorerItem item, HashSet<string> states)
    {
        if (item.IsFolder && item.IsExpanded)
        {
            string fullPath = string.IsNullOrEmpty(item.CatalogPath) ? item.Name : $"{item.CatalogPath}{AppConstants.CatalogDelimeter}{item.Name}";
            states.Add(fullPath);
        }
        if (item.Children != null)
        {
            foreach (var child in item.Children) SaveExpandedState(child, states);
        }
    }

    /// <summary>
    /// Loads passwords and returns a list of that
    /// </summary>
    /// <returns></returns>
    private async Task<IEnumerable<PangoExplorerItem>> LoadPasswordsAsync()
    {
        if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebug("Loading passwords...");

        var queryResult = await _sender.Send<ErrorOr<IEnumerable<PangoPasswordListItemDto>>>(new UserPasswordsQuery());

        if (queryResult.IsError)
        {
            if (Logger.IsEnabled(LogLevel.Error)) Logger.LogError("Loaded passwords failed: {Error}", queryResult.FirstError);
            return [];
        }
        else
        {
            var items = queryResult.Value.Adapt<IEnumerable<PangoExplorerItem>>().ToList();

            foreach (var item in items)
            {
                var originalDto = queryResult.Value.FirstOrDefault(x => x.Id == item.Id);
                if (originalDto != null && originalDto.Properties != null)
                {
                    item.Properties = originalDto.Properties;

                    if (originalDto.Properties.TryGetValue(PasswordProperties.ExpirationDate, out var expStr))
                    {
                        if (DateTimeOffset.TryParse(expStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var expDate))
                        {
                            item.ExpirationDate = expDate;
                        }
                    }
                }
            }

            if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebug("Loaded {Count} passwords", items.Count(p => !p.IsFolder));

            return items.OrderBy(i => i.NestingLevel);
        }
    }

    /// <summary>
    /// Caches flat representation of passwords to drive initial screen visibility
    /// </summary>
    private void SetOriginalList(IEnumerable<PangoExplorerItem> passwords)
    {
        _originalList ??= [];
        if (_originalList.Count > 0) _originalList.Clear();
        foreach (var pwd in passwords) _originalList.Add(pwd);
    }

    /// <summary>
    /// Displays <paramref name="passwords"/> in a tree view and checks expirations safely
    /// </summary>
    private async Task DisplayPasswordsInTreeAsync(IEnumerable<PangoExplorerItem> passwords, HashSet<string> expandedPaths)
    {
        int warningDays = 7;
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(Constants.Settings.ExpirationWarningDays, out object? warningObj) && warningObj != null)
        {
            warningDays = Convert.ToInt32(warningObj);
        }
        var now = DateTime.Now.Date;

        var (rootItems, userExpirations) = await Task.Run(() =>
        {
            var folderMap = new Dictionary<string, PangoExplorerItem>(StringComparer.OrdinalIgnoreCase);
            var childrenMap = new Dictionary<string, List<PangoExplorerItem>>(StringComparer.OrdinalIgnoreCase);
            var rootItemsList = new List<PangoExplorerItem>();

            foreach (var pwd in passwords)
            {
                if (pwd.IsFolder)
                {
                    string fullPath = string.IsNullOrEmpty(pwd.CatalogPath) ? pwd.Name : $"{pwd.CatalogPath}{AppConstants.CatalogDelimeter}{pwd.Name}";
                    if (expandedPaths.Contains(fullPath)) pwd.IsExpanded = true;
                    folderMap[fullPath] = pwd;
                    if (!childrenMap.ContainsKey(fullPath)) childrenMap[fullPath] = [];
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
                    if (childrenMap.TryGetValue(pwd.CatalogPath, out var list)) list.Add(pwd);
                    else rootItemsList.Add(pwd);
                }
            }

            SortAndBind(rootItemsList, childrenMap, null);

            var allItems = new List<PangoExplorerItem>();
            foreach (var root in rootItemsList)
            {
                allItems.Add(root);
                allItems.AddRange(root.GetAllDescendants());
            }

            var expirationsCache = new List<ExpirationCacheItem>();
            foreach (var item in allItems.Where(p => !p.IsFolder))
            {
                if (item.ExpirationDate.HasValue)
                {
                    var expDate = item.ExpirationDate.Value.LocalDateTime.Date;
                    int daysLeft = (int)(expDate - now).TotalDays;
                    string dateStr = expDate.ToString("d", System.Globalization.CultureInfo.CurrentCulture);

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

            return (rootItemsList, expirationsCache);
        });

        Passwords.Clear();
        foreach (var rootItem in rootItems)
        {
            Passwords.Add(rootItem);
        }

        UpdateUserExpirationCache(userExpirations);
    }

    /// <summary>
    /// Recursively sorts the tree structure and binds parents.
    /// </summary>
    private static void SortAndBind(List<PangoExplorerItem> items, Dictionary<string, List<PangoExplorerItem>> childrenMap, PangoExplorerItem? parent)
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
                string fullPath = string.IsNullOrEmpty(item.CatalogPath) ? item.Name : $"{item.CatalogPath}{AppConstants.CatalogDelimeter}{item.Name}";
                if (childrenMap.TryGetValue(fullPath, out var children))
                {
                    SortAndBind(children, childrenMap, item);
                    item.Children = new ObservableCollection<PangoExplorerItem>(children);
                }
            }
        }
    }

    /// <summary>
    /// Saves the expiration dates cache to disk for the Background Notification Service to use.
    /// </summary>
    private void UpdateUserExpirationCache(List<ExpirationCacheItem> newItems)
    {
        try
        {
            string configDir = ApplicationData.Current.LocalFolder.Path;
            string cacheFile = System.IO.Path.Combine(configDir, "expiration_cache.json");
            Dictionary<string, List<ExpirationCacheItem>> cache = [];

            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (System.IO.File.Exists(cacheFile))
            {
                try
                {
                    string existingJson = System.IO.File.ReadAllText(cacheFile).Trim();
                    if (existingJson.StartsWith('{') && existingJson.EndsWith('}'))
                    {
                        cache = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<ExpirationCacheItem>>>(existingJson, options) ?? [];
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                }
            }

            string currentUser = _userContextProvider.GetUserName();
            if (!string.IsNullOrEmpty(currentUser))
            {
                cache[currentUser] = newItems;
                System.IO.File.WriteAllText(cacheFile, System.Text.Json.JsonSerializer.Serialize(cache, options));
            }

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage("CACHE_UPDATED", AppNotificationType.Info));
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Warning)) Logger.LogWarning(ex, "Cannot save expiration cache to disk");
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

    /// <summary>
    /// Removes the <paramref name="password"/> from the tree collection of <paramref name="passwords"/>
    /// </summary>
    /// <param name="passwords"></param>
    /// <param name="password"></param>
    private bool RemovePassword(ObservableCollection<PangoExplorerItem> passwords, PangoExplorerItem password)
    {
        if (passwords is null || !passwords.Any() || password is null) return false;

        PangoExplorerItem? passwordToRemove = passwords.FirstOrDefault(p => p.Id == password.Id);

        if (passwordToRemove is not null)
        {
            var originalItemToRemove = _originalList.FirstOrDefault(p => p.Id.Equals(passwordToRemove.Id));
            if (originalItemToRemove != null) _originalList.Remove(originalItemToRemove);

            passwords.Remove(passwordToRemove);
            return true;
        }
        else
        {
            foreach (var p in passwords)
            {
                if (RemovePassword(p.Children, password)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Updates passwords to commit movement of <paramref name="movedItem"/> to the <paramref name="newParent"/>
    /// </summary>
    public async Task CommitPasswordMovementAsync(PangoExplorerItem movedItem, PangoExplorerItem? newParent)
    {
        movedItem.Parent = newParent;
        movedItem.RecalculateCatalogPath();

        Dictionary<Guid, string> passwordItemsToUpdate = BuildPasswordAndCatalogPathPairs([movedItem]);

        if (passwordItemsToUpdate.Count > 0)
        {
            await _sender.Send(new MovePasswordsToCatalogCommand(passwordItemsToUpdate));
        }

        await ResetViewAsync();
    }

    /// <summary>
    /// Retrieves list of Id+CatalogPath of all passwords from the <paramref name="itemsSource"/> of tree structure
    /// </summary>
    /// <param name="itemsSource">List of items in tree structure</param>
    /// <returns>Passwords Id+CatalogPath, created from the passed <paramref name="itemsSource"/></returns>
    private static Dictionary<Guid, string> BuildPasswordAndCatalogPathPairs(List<PangoExplorerItem> itemsSource)
    {
        Dictionary<Guid, string> result = [];
        foreach (PangoExplorerItem treeItem in itemsSource)
        {
            result.Add(treeItem.Id, treeItem.CatalogPath);
            if (treeItem.Children.Any())
            {
                foreach (KeyValuePair<Guid, string> itemToAdd in BuildPasswordAndCatalogPathPairs([.. treeItem.Children]))
                {
                    result.Add(itemToAdd.Key, itemToAdd.Value);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Returns a password that fits <paramref name="predicate"/>
    /// </summary>
    /// <param name="items"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    private static PangoExplorerItem? FindPassword(IEnumerable<PangoExplorerItem> items, Func<PangoExplorerItem, bool> predicate)
    {
        if (items is null || !items.Any()) return null;

        foreach (var item in items)
        {
            if (predicate(item)) return item;
            var found = FindPassword(item.Children, predicate);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Returns a list of all currently existing catalogs
    /// </summary>
    /// <returns></returns>
    private List<string> GetAvailableCatalogs()
    {
        List<string> catalogs =
            [..Passwords.FindItems(p => p.Type == PangoExplorerItem.ExplorerItemType.Folder)
            .Select(p => PasswordPathUtility.BuildCatalogPath(p.CatalogPath, p.Name))
            .OrderBy(p => p)];

        if (catalogs.Count != 0) catalogs.Insert(0, string.Empty);
        return catalogs;
    }

    /// <summary>
    /// Returns the currently selected folder path
    /// </summary>
    /// <returns></returns>
    private string GetPathToSelectedFolder()
        => SelectedItem == null ? string.Empty :
            SelectedItem.Type == PangoExplorerItem.ExplorerItemType.Folder ?
            PasswordPathUtility.BuildCatalogPath(SelectedItem.CatalogPath, SelectedItem.Name) :
            SelectedItem.CatalogPath;

    /// <summary>
    /// Opens EditPasswordView with generated password value
    /// </summary>
    /// <returns></returns>
    private void OpenEditForGeneratedPassword(string generatedPassword)
    {
        WeakReferenceMessenger.Default.Send(
            new NavigationRequstedMessage(
                new NavigationParameters(
                    AppView.EditPassword,
                    AppView.PasswordsIndex,
                    new EditPasswordParameters(
                        isNew: true,
                        catalog: GetPathToSelectedFolder(),
                        selectedPasswordId: null,
                        availableCatalogs: GetAvailableCatalogs(),
                        generatedPassword: generatedPassword))));
    }

    #endregion
}
