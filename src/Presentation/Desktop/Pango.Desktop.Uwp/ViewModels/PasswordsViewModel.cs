using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using Mapster;
using MediatR;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.DeletePassword;
using Pango.Application.UseCases.Password.Queries.FindUserPassword;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Dialogs;
using Pango.Desktop.Uwp.Dialogs.Views;
using Pango.Desktop.Uwp.Models;
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

    public PasswordsViewModel(ISender sender, IDialogService dialogService)
    {
        _sender = sender;
        _dialogService = dialogService;
        Passwords = new();

        CreatePasswordCommand = new RelayCommand(OnCreatePassword);
        CreateCatalogCommand = new RelayCommand(OnCreateCatalogAsync);
        EditPasswordCommand = new RelayCommand<PasswordExplorerItem>(OnEditPassword);
        DeletePasswordCommand = new RelayCommand<PasswordExplorerItem>(OnDeletePassword);
        CopyPasswordToClipboardCommand = new RelayCommand<PasswordExplorerItem>(OnCopyPasswordToClipboard);


        WeakReferenceMessenger.Default.Register<PasswordCreatedMessage>(this, OnPasswordCreated);
    }


    #region Commands

    public RelayCommand CreatePasswordCommand { get; }
    public RelayCommand CreateCatalogCommand { get; }
    public RelayCommand<PasswordExplorerItem> EditPasswordCommand { get; }
    public RelayCommand<PasswordExplorerItem> DeletePasswordCommand { get; }
    public RelayCommand<PasswordExplorerItem> CopyPasswordToClipboardCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<PasswordExplorerItem> Passwords { get; private set; }

    public PasswordExplorerItem SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public bool HasPasswords
    {
        get => _hasPasswords;
        set => SetProperty(ref _hasPasswords, value);
    }

    #endregion

    public override async Task OnNavigatedToAsync(object parameter)
    {
        await LoadPasswords();
    }

    private async Task LoadPasswords()
    {
        var queryResult = await _sender.Send<ErrorOr<IEnumerable<PangoPasswordListItemDto>>>(new UserPasswordsQuery());

        var passwordsOrderedList = queryResult.Value.OrderBy(p => p.IsCatalog).ThenBy(p => p.Name);

        Passwords.Clear();
        foreach (var pwd in passwordsOrderedList)
        {
            var item = pwd.Adapt<PasswordExplorerItem>();
            Passwords.Add(item);
        }

        HasPasswords = Passwords.Any();
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

    private async void OnDeletePassword(PasswordExplorerItem dto)
    {
        string confirmationTitle = ViewResourceLoader.GetString("Confirm_PasswordDeletion");
        string confirmationDescription = ViewResourceLoader.GetString("Confirm_PasswordDeletionDetails");

        bool deletionConfirmed = await _dialogService.ConfirmAsync(confirmationTitle, confirmationDescription);

        if (!deletionConfirmed)
        {
            return;
        }

        var result = await _sender.Send(new DeletePasswordCommand(dto.Id));

        if (!result.IsError)
        {
            Passwords.Remove(dto);
            HasPasswords = Passwords.Any();
        }
    }

    private void OnEditPassword(PasswordExplorerItem selected)
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.EditPassword, selected?.Id)));
    }

    private void OnCreatePassword()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.EditPassword)));
    }

    private async void OnCreateCatalogAsync()
    {
        string initialPath = SelectedItem == null ? null : 
            SelectedItem.Type == PasswordExplorerItem.ExplorerItemType.Folder ? 
            SelectedItem.CatalogPath + $"/{SelectedItem.Name}" : 
            SelectedItem.CatalogPath;

        await _dialogService.ShowAsync(new NewPasswordCatalogDialog(initialPath));
    }

    private void OnPasswordCreated(object recipient, PasswordCreatedMessage message)
    {
        if (string.IsNullOrEmpty(message.Value.CatalogPath))
        {
            Passwords.Add(message.Value.Adapt<PasswordExplorerItem>());
        }
        else
        {
            AddPassword(Passwords, message.Value.Adapt<PasswordExplorerItem>(), new Queue<string>(message.Value.CatalogPath.Split('/')));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="passwords"></param>
    /// <param name="password"></param>
    /// <param name="catalogs"></param>
    /// <returns></returns>
    private bool AddPassword(ObservableCollection<PasswordExplorerItem> passwords, PasswordExplorerItem password, Queue<string> catalogs)
    {
        if(catalogs is null || catalogs.Count == 0)
        { 
            return false; 
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
            catalog.Children.Add(password);
            return true;
        }
    }
}
