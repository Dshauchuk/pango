using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.DeletePassword;
using Pango.Application.UseCases.Password.Commands.UpdatePasswordsCatalogPath;
using Pango.Application.UseCases.Password.Queries.FindUserPassword;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Extensions;
using Pango.Desktop.Uwp.Dialogs;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Models.Parameters;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.PasswordsIndex)]
public sealed class PasswordsViewModel : ViewModelBase
{
    private readonly ISender _sender;
    private readonly IDialogService _dialogService;
    private bool _hasPasswords;
    private PangoExplorerItem? _selectedItem;
    private ObservableCollection<PangoExplorerItem> _originalList;
    private string _searchText = string.Empty;

    public PasswordsViewModel(ISender sender, IDialogService dialogService, ILogger<PasswordsViewModel> logger) : base(logger)
    {
        _sender = sender;
        _dialogService = dialogService;

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

    public bool ShowInitialScreen => !HasPasswords && !_originalList.Any();

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    #endregion

    #region Overrides

    protected override void RegisterMessages()
    {
        base.RegisterMessages();

        WeakReferenceMessenger.Default.Register<PasswordCreatedMessage>(this, OnPasswordCreated);
        WeakReferenceMessenger.Default.Register<PasswordUpdatedMessage>(this, OnPasswordUpdatedAsync);
    }

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        await ResetViewAsync();
    }

    #endregion

    #region Event&Command Handlers

    private async void Current_LoginSucceeded(string userId)
    {
        await ResetViewAsync();
    }

    private async void OnSeePasswordCommand(PangoExplorerItem? item)
    {
        if (item != null)
        {
            await ShowPasswordDetailsAsync(item);
        }
    }

    private void Passwords_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SelectedItem = null;
        HasPasswords = Passwords.Any(p => p.IsVisible);
    }

    private void OnPasswordCreated(object recipient, PasswordCreatedMessage message)
    {
        AddPassword(Passwords, message.Value.Adapt<PangoExplorerItem>(), message.Value.CatalogPath?.ParseCatalogPath());
    }

    private async void OnPasswordUpdatedAsync(object recipient, PasswordUpdatedMessage message)
    {
        await ResetViewAsync();
    }

    private async void OnCopyPasswordToClipboard(PangoExplorerItem? dto)
    {
        if(dto is null)
        {
            return;
        }

        var passwordResult = await _sender.Send(new FindUserPasswordQuery(dto.Id));

        if (!passwordResult.IsError)
        {
            DataPackage dataPackage = new()
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            dataPackage.SetText(passwordResult.Value.Value?.ToString() ?? string.Empty);

            Clipboard.SetContent(dataPackage);

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(ViewResourceLoader.GetString("PasswordCopiedToClipboard")));
        }
    }

    private async void OnDeleteAsync(PangoExplorerItem? dto)
    {
        if (dto is null)
        {
            return;
        }

        string confirmationTitle = ViewResourceLoader.GetString("Confirm_PasswordDeletion");
        string confirmationDescription;
        string completionMessage;

        if (dto.Type == PangoExplorerItem.ExplorerItemType.Folder)
        {
            confirmationDescription = string.Format(ViewResourceLoader.GetString("RemoveCatalog_Message"), dto.Name, dto.Children?.Count ?? 0);
            completionMessage = string.Format(ViewResourceLoader.GetString("CatalogDeleted_FormattedMessage"), dto.Name);
        }
        else
        {
            confirmationDescription = ViewResourceLoader.GetString("Confirm_PasswordDeletionDetails");
            completionMessage = string.Format(ViewResourceLoader.GetString("PasswordDeleted_Format"), dto.Name);
        }

        bool deletionConfirmed = await _dialogService.ConfirmAsync(confirmationTitle, confirmationDescription);

        if (!deletionConfirmed)
        {
            return;
        }

        var result = await _sender.Send(new DeletePasswordCommand(dto.Id));

        if(result.IsError)
        {
            Logger.LogError($"Deleting {(dto.Type == PangoExplorerItem.ExplorerItemType.File ? "password" : "catalog")} \"{dto.Name}\" failed: {result.FirstError}");
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(completionMessage));
        }
        else
        {
            RemovePassword(Passwords, dto);

            Logger.LogDebug($"{(dto.Type == PangoExplorerItem.ExplorerItemType.File ? "Password" : "Catalog")} \"{dto.Name}\" has been successfully deleted");
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(completionMessage));
        }
    }

    private async void OnEditPasswordAsync(PangoExplorerItem? selected)
    {
        if(selected is null)
        {
            return;
        }

        if(selected.Type == PangoExplorerItem.ExplorerItemType.Folder)
        {
            await _dialogService
                .ShowNewCatalogDialogAsync(
                    new EditCatalogParameters(GetAvailableCatalogs(), GetPathToSelectedFolder(), selected, (selected?.Parent?.Children ?? Passwords)?.Select(c => c.Name).ToList() ?? []));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.EditPassword, AppView.PasswordsIndex, new EditPasswordParameters(false, null, selected?.Id, GetAvailableCatalogs()))));
        }
    }

    private void OnCreatePassword()
    {
        WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.EditPassword, AppView.PasswordsIndex, new EditPasswordParameters(true, GetPathToSelectedFolder(), null, GetAvailableCatalogs()))));
    }

    private async void OnCreateCatalogAsync()
    {
        await _dialogService
            .ShowNewCatalogDialogAsync(
                new EditCatalogParameters(GetAvailableCatalogs(), GetPathToSelectedFolder(), null, (SelectedItem?.Children ?? Passwords)?.Select(c => c.Name).ToList() ?? []));
    }

    private async void OnUpdateListAsync()
    {
        await ResetViewAsync();
    }
    #endregion

    #region Public Methods

    public async Task ShowPasswordDetailsAsync(PangoExplorerItem selectedPassword)
    {
        await _dialogService.ShowPasswordDetailsAsync(new PasswordDetailsParameters(selectedPassword.Id, GetAvailableCatalogs()));
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles the command to filter passwords content
    /// </summary>
    /// <param name="searchText"></param>
    private void OnFilterAsync(string? searchText)
    {
        Func<PangoExplorerItem, bool> searchPredicate = string.IsNullOrEmpty(searchText) ? (i) => true : (i) => i.Name.Contains(searchText);
        foreach(PangoExplorerItem password in Passwords)
        {
            Filter(password, searchPredicate);
        }

        HasPasswords = Passwords.Any(p => p.IsVisible);
    }

    /// <summary>
    /// Returns true and makes the <paramref name="node"/> visible if it or any its children fits <paramref name="searchPredicate"/>, otherwise - false. 
    /// </summary>
    /// <param name="node"></param>
    /// <param name="searchPredicate"></param>
    /// <returns></returns>
    private static bool Filter(PangoExplorerItem node, Func<PangoExplorerItem, bool> searchPredicate)
    {
        if(node is null)
        {
            return false;
        }

        if(node.Type == PangoExplorerItem.ExplorerItemType.File)
        {
            return node.IsVisible = searchPredicate(node);
        }
        else
        {
            bool hasVisibleItems = false;

            if (node.Children.Any())
            {
                foreach(PangoExplorerItem item in node.Children)
                {
                    if (Filter(item, searchPredicate) && !hasVisibleItems)
                    {
                        hasVisibleItems = true;
                    }
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
    private bool CanDelete(PangoExplorerItem? item)
    {
        return item != null;
    }

    /// <summary>
    /// Returns true if <paramref name="item"/> can be edited, otherwise - false
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private bool CanEdit(PangoExplorerItem? item)
    {
        return item != null;
    }

    /// <summary>
    /// Resets the view
    /// </summary>
    /// <returns></returns>
    private async Task ResetViewAsync()
    {
        SelectedItem = null;
        SearchText = string.Empty;

        IEnumerable<PangoExplorerItem> passwords = await LoadPasswordsAsync();

        DisplayPasswordsInTree(passwords);
        SetOriginalList(passwords);
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
    /// Saves the list of <paramref name="passwords"/> into a buffer
    /// </summary>
    /// <param name="passwords"></param>
    private void SetOriginalList(IEnumerable<PangoExplorerItem> passwords)
    {
        _originalList ??= [];

        if (_originalList.Any())
        {
            _originalList.Clear();
        }

        foreach (var pwd in passwords)
        {
            AddPassword(_originalList, pwd, pwd.CatalogPath.ParseCatalogPath());
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
        if(catalogs is null || catalogs.Count == 0)
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

    /// <summary>
    /// Removes the <paramref name="password"/> from the tree collection of <paramref name="passwords"/>
    /// </summary>
    /// <param name="passwords"></param>
    /// <param name="password"></param>
    private bool RemovePassword(ObservableCollection<PangoExplorerItem> passwords, PangoExplorerItem password)
    {
        if(passwords is null || !passwords.Any() || password is null)
        {
            return false;
        }

        PangoExplorerItem? passwordToRemove = passwords.FirstOrDefault(p => p.Id == password.Id);

        if(passwordToRemove is not null)
        {
            // remove it from the original list firstly
            // becase the removing from the displayed collection triggers notification of change of HasPasswords prop
            var originalItemToRemove = _originalList.FirstOrDefault(p => p.Id.Equals(passwordToRemove.Id));
            if (originalItemToRemove != null)
            {
                _originalList.Remove(originalItemToRemove);
            }

            passwords.Remove(passwordToRemove);

            return true;
        }
        else
        {
            foreach(var p in passwords)
            {
                if(RemovePassword(p.Children, password))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Saves all <see cref="Passwords"/> in the current state
    /// </summary>
    public async Task UpdatePasswordItemsInTreeAsync()
    {
        List<PangoPasswordListItemDto> allPasswordDtosFromTree = BuildPasswordDtosFromTree(Passwords.ToList(), string.Empty);
        List<PangoPasswordListItemDto> allOriginalPasswordDtosFromTree = BuildPasswordDtosFromTree(_originalList.ToList(), string.Empty);
        // find all passwords with updated catalog paths
        List<PangoPasswordListItemDto> passwordListItemDtosToUpdate = allPasswordDtosFromTree
            .Where(pd => allOriginalPasswordDtosFromTree.FirstOrDefault(ol => ol.Id == pd.Id)?.CatalogPath != pd.CatalogPath).ToList();

        if (passwordListItemDtosToUpdate.Count > 0)
        {
            await _sender.Send(new UpdatePasswordsCatalogPathCommand(passwordListItemDtosToUpdate.ToDictionary(k => k.Id, v => v.CatalogPath)));
            SetOriginalList(allPasswordDtosFromTree.Adapt<IEnumerable<PangoExplorerItem>>());
        }
    }

    /// <summary>
    /// Retrieves list of all passwords from the <paramref name="itemsSource"/> of tree structure
    /// </summary>
    /// <param name="itemsSource">List of items in tree structure</param>
    /// <param name="baseCatalogPath">Catalog path for the first nesting level of items in tree</param>
    /// <returns>List of passwords, created from the passed <paramref name="itemsSource"/></returns>
    private static List<PangoPasswordListItemDto> BuildPasswordDtosFromTree(List<PangoExplorerItem> itemsSource, string baseCatalogPath)
    {
        List<PangoPasswordListItemDto> result = new();

        foreach (PangoExplorerItem treeItem in itemsSource)
        {
            result.Add(new PangoPasswordListItemDto()
            {
                Id = treeItem.Id,
                CatalogPath = baseCatalogPath,
                IsCatalog = treeItem.Type == PangoExplorerItem.ExplorerItemType.Folder,
                Name = treeItem.Name,
                //TODO: change CreatedAt and LastModifiedAt to the real dates
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            });

            if (treeItem.Children.Any())
            {
                string childrenBaseCatalogPath = $"{baseCatalogPath}{(string.IsNullOrEmpty(baseCatalogPath) ? string.Empty : AppConstants.CatalogDelimeter)}{treeItem.Name}";
                result.AddRange(BuildPasswordDtosFromTree(treeItem.Children.ToList(), childrenBaseCatalogPath));
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
    private PangoExplorerItem? FindPassword(IEnumerable<PangoExplorerItem> items, Func<PangoExplorerItem, bool> predicate)
    {
        if(items is null || !items.Any())
        {
            return null;
        }

        foreach(var item in items)
        {
            if (predicate(item))
            {
                return item;
            }

            var found = FindPassword(item.Children, predicate);

            if (found != null)
            {
                return found;
            }
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
            .Select(p => string.IsNullOrEmpty(p.CatalogPath) ? p.Name : $"{p.CatalogPath}{AppConstants.CatalogDelimeter}{p.Name}")
            .OrderBy(p => p)];

        if (catalogs.Any())
        {
            catalogs.Insert(0, string.Empty);
        }

        return catalogs;
    }

    /// <summary>
    /// Returns the currently selected folder path
    /// </summary>
    /// <returns></returns>
    private string GetPathToSelectedFolder()
        => SelectedItem == null ? string.Empty :
            SelectedItem.Type == PangoExplorerItem.ExplorerItemType.Folder ?
            SelectedItem.CatalogPath + (string.IsNullOrEmpty(SelectedItem.CatalogPath) ? string.Empty : AppConstants.CatalogDelimeter) + SelectedItem.Name :
            SelectedItem.CatalogPath;

    #endregion
}
