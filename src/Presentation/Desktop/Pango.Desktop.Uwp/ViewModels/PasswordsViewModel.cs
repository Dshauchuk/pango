﻿using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.DeletePassword;
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
    private PasswordExplorerItem _selectedItem;
    private ObservableCollection<PasswordExplorerItem> _originalList;
    private string _searchText;

    public PasswordsViewModel(ISender sender, IDialogService dialogService, ILogger<PasswordsViewModel> logger) : base(logger)
    {
        _sender = sender;
        _dialogService = dialogService;

        _originalList = [];
        Passwords = [];
        Passwords.CollectionChanged += Passwords_CollectionChanged;

        SearchCommand = new RelayCommand<string>(OnSearchAsync);
        CreatePasswordCommand = new RelayCommand(OnCreatePassword);
        CreateCatalogCommand = new RelayCommand(OnCreateCatalogAsync);
        DeleteCommand = new RelayCommand<PasswordExplorerItem>(OnDeleteAsync, CanDelete);
        EditPasswordCommand = new RelayCommand<PasswordExplorerItem>(OnEditPasswordAsync, CanEdit);
        CopyPasswordToClipboardCommand = new RelayCommand<PasswordExplorerItem>(OnCopyPasswordToClipboard);

        WeakReferenceMessenger.Default.Register<PasswordCreatedMessage>(this, OnPasswordCreated);
        WeakReferenceMessenger.Default.Register<PasswordUpdatedMessage>(this, OnPasswordUpdatedAsync);
    }

    #region Commands

    public RelayCommand<PasswordExplorerItem> DeleteCommand { get; }
    public RelayCommand CreateCatalogCommand { get; }
    public RelayCommand CreatePasswordCommand { get; }
    public RelayCommand<string> SearchCommand { get; }
    public RelayCommand<PasswordExplorerItem> EditPasswordCommand { get; }
    public RelayCommand<PasswordExplorerItem> CopyPasswordToClipboardCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<PasswordExplorerItem> Passwords { get; private set; }

    public PasswordExplorerItem SelectedItem
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

    public override async Task OnNavigatedToAsync(object parameter)
    {
        await ResetViewAsync();
    }

    #endregion

    #region Event&Command Handlers

    private void Passwords_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SelectedItem = null;
        HasPasswords = Passwords.Any();
    }

    private void OnPasswordCreated(object recipient, PasswordCreatedMessage message)
    {
        AddPassword(Passwords, message.Value.Adapt<PasswordExplorerItem>(), message.Value.CatalogPath?.ParseCatalogPath());
    }

    private async void OnPasswordUpdatedAsync(object recipient, PasswordUpdatedMessage message)
    {
        await ResetViewAsync();
    }

    private async void OnCopyPasswordToClipboard(PasswordExplorerItem dto)
    {
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

    private async void OnDeleteAsync(PasswordExplorerItem dto)
    {
        string confirmationTitle = ViewResourceLoader.GetString("Confirm_PasswordDeletion");
        string confirmationDescription;

        if (dto.Type == PasswordExplorerItem.ExplorerItemType.Folder)
        {
            confirmationDescription = string.Format(ViewResourceLoader.GetString("RemoveCatalog_Message"), dto.Name, dto.Children?.Count ?? 0);
        }
        else
        {
            confirmationDescription = ViewResourceLoader.GetString("Confirm_PasswordDeletionDetails");
        }

        bool deletionConfirmed = await _dialogService.ConfirmAsync(confirmationTitle, confirmationDescription);

        if (!deletionConfirmed)
        {
            return;
        }

        var result = await _sender.Send(new DeletePasswordCommand(dto.Id));

        if(result.IsError)
        {
            Logger.LogError($"Deleting {(dto.Type == PasswordExplorerItem.ExplorerItemType.File ? "password" : "catalog")} \"{dto.Name}\" failed: {result.FirstError}");
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(string.Format(ViewResourceLoader.GetString("PasswordDeletetionFailed_Format"), dto.Name)));
        }
        else
        {
            RemovePassword(Passwords, dto);

            Logger.LogDebug($"{(dto.Type == PasswordExplorerItem.ExplorerItemType.File ? "Password" : "Catalog")} \"{dto.Name}\" has been successfully deleted");
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(string.Format(ViewResourceLoader.GetString("PasswordDeleted_Format"), dto.Name)));
        }
    }

    private async void OnEditPasswordAsync(PasswordExplorerItem selected)
    {
        if(selected is null)
        {
            return;
        }

        if(selected.Type == PasswordExplorerItem.ExplorerItemType.Folder)
        {
            await _dialogService
                .ShowNewCatalogDialog(
                    new EditCatalogParameters(GetAvailableCatalogs(), GetPathToSelectedFolder(), selected, (selected?.Parent?.Children ?? Passwords)?.Select(c => c.Name).ToList() ?? []));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.EditPassword, new EditPasswordParameters(false, null, selected?.Id, GetAvailableCatalogs()))));
        }
    }

    private void OnCreatePassword()
    {
        WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.EditPassword, new EditPasswordParameters(true, GetPathToSelectedFolder(), null, GetAvailableCatalogs()))));
    }

    private async void OnCreateCatalogAsync()
    {
        await _dialogService
            .ShowNewCatalogDialog(
                new EditCatalogParameters(GetAvailableCatalogs(), GetPathToSelectedFolder(), null, (SelectedItem?.Children ?? Passwords)?.Select(c => c.Name).ToList() ?? []));
    }

    #endregion

    #region Private Methods

    private void OnSearchAsync(string searchText)
    {
        Func<PasswordExplorerItem, bool> searchPredicate = string.IsNullOrEmpty(searchText) ? (i) => true : (i) => i.Name.Contains(searchText);

        IEnumerable<PasswordExplorerItem> foundItems = Search(_originalList, searchPredicate);

        DisplayPasswords(foundItems);
    }

    private IEnumerable<PasswordExplorerItem> Search(IEnumerable<PasswordExplorerItem> items, Func<PasswordExplorerItem, bool> searchPredicate)
    {
        if (items == null || !items.Any())
        {
            return [];
        }

        List<PasswordExplorerItem> foundItems = [];

        foreach (var item in items)
        {
            IEnumerable<PasswordExplorerItem> foundChildren = Search(item.Children, searchPredicate);

            if (foundChildren.Any() || searchPredicate(item))
            {
                if(item.Children == null)
                {
                    item.Children = [];
                }
                else
                {
                    item.Children.Clear();
                }

                foreach(var child in foundChildren)
                {
                    item.Children.Add(child);
                }

                foundItems.Add(item);
            }
        }

        return foundItems;
    }

    private bool CanDelete(PasswordExplorerItem item)
    {
        return item != null;
    }

    private bool CanEdit(PasswordExplorerItem item)
    {
        return item != null;
    }

    private async Task ResetViewAsync()
    {
        SelectedItem = null;
        SearchText = string.Empty;

        IEnumerable<PasswordExplorerItem> passwords = await LoadPasswordsAsync();

        DisplayPasswords(passwords);
        SetOriginalList(passwords);
    }

    private async Task<IEnumerable<PasswordExplorerItem>> LoadPasswordsAsync()
    {
        Logger.LogDebug($"Loading passwords...");
        var queryResult = await _sender.Send<ErrorOr<IEnumerable<PangoPasswordListItemDto>>>(new UserPasswordsQuery());

        if (queryResult.IsError)
        {
            Logger.LogError($"Loaded passwords failed: {queryResult.FirstError}");
            return [];
        }
        else
        {
            Logger.LogDebug($"Loaded {queryResult.Value.Count(p => !p.IsCatalog)} passwords");
            return queryResult.Value.Adapt<IEnumerable<PasswordExplorerItem>>().OrderBy(i => i.NestingLevel);
        }
    }

    private void SetOriginalList(IEnumerable<PasswordExplorerItem> passwords)
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

    private void DisplayPasswords(IEnumerable<PasswordExplorerItem> passwords)
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
    private bool AddPassword(ObservableCollection<PasswordExplorerItem> passwords, PasswordExplorerItem password, Queue<string> catalogs)
    {
        if(catalogs is null || catalogs.Count == 0)
        {
            Insert(passwords, password);
            return true; 
        }

        string catalogName = catalogs.Dequeue();
        PasswordExplorerItem catalog = passwords.FirstOrDefault(p => p.Type == PasswordExplorerItem.ExplorerItemType.Folder && p.Name == catalogName);

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

    private void Insert(ObservableCollection<PasswordExplorerItem> passwords, PasswordExplorerItem password)
    {
        if (!passwords.Any())
        {
            passwords.Add(password);
            return;
        }

        int index = 0;

        if (password.Type == PasswordExplorerItem.ExplorerItemType.File)
        {
            for (; index < passwords.Count; index++)
            {
                if (passwords[index].Type == PasswordExplorerItem.ExplorerItemType.File)
                {
                    break;
                }
            }
        }

        for (; index < passwords.Count; index++)
        {
            if (passwords[index].Type == PasswordExplorerItem.ExplorerItemType.File && password.Type == PasswordExplorerItem.ExplorerItemType.Folder)
            {
                break;
            }

            if (passwords[index].Name.CompareTo(password.Name) < 0)
            {
                continue;
            }
            else
            {
                break;
            }
        }

        passwords.Insert(index, password);
    }

    /// <summary>
    /// Removes the <paramref name="password"/> from the tree collection of <paramref name="passwords"/>
    /// </summary>
    /// <param name="passwords"></param>
    /// <param name="password"></param>
    private bool RemovePassword(ObservableCollection<PasswordExplorerItem> passwords, PasswordExplorerItem password)
    {
        if(passwords is null || !passwords.Any() || password is null)
        {
            return false;
        }

        PasswordExplorerItem passwordToRemove = passwords.FirstOrDefault(p => p.Id == password.Id);

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

    private PasswordExplorerItem? FindPassword(IEnumerable<PasswordExplorerItem> items, Func<PasswordExplorerItem, bool> predicate)
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
            [..Passwords.FindItems(p => p.Type == PasswordExplorerItem.ExplorerItemType.Folder)
            .Select(p => string.IsNullOrEmpty(p.CatalogPath) ? p.Name : $"{p.CatalogPath}{AppConstants.CatalogDelimeter}{p.Name}")
            .OrderBy(p => p)];

        if (catalogs.Any())
        {
            catalogs.Insert(0, "");
        }

        return catalogs;
    }

    /// <summary>
    /// Returns the currently selected folder path
    /// </summary>
    /// <returns></returns>
    private string GetPathToSelectedFolder()
        => SelectedItem == null ? null :
            SelectedItem.Type == PasswordExplorerItem.ExplorerItemType.Folder ?
            SelectedItem.CatalogPath + (string.IsNullOrEmpty(SelectedItem.CatalogPath) ? string.Empty : AppConstants.CatalogDelimeter) + SelectedItem.Name :
            SelectedItem.CatalogPath;

    #endregion
}
