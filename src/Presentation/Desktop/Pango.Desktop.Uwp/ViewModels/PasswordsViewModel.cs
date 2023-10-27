using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.DeletePassword;
using Pango.Application.UseCases.Password.Queries.FindUserPassword;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
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
    private bool _hasPasswords;

    public PasswordsViewModel(ISender sender)
    {
        _sender = sender;
        Passwords = new();

        CreatePasswordCommand = new RelayCommand(OnCreatePassword);
        DeletePasswordCommand = new RelayCommand<PasswordListItemDto>(OnDeletePassword);
        CopyPasswordToClipboardCommand = new RelayCommand<PasswordListItemDto>(OnCopyPasswordToClipboard);
    }

    #region Commands

    public RelayCommand CreatePasswordCommand { get; }
    public RelayCommand<PasswordListItemDto> DeletePasswordCommand { get; }
    public RelayCommand<PasswordListItemDto> CopyPasswordToClipboardCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<PasswordListItemDto> Passwords { get; private set; }

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
        var queryResult = await _sender.Send<ErrorOr<IEnumerable<PasswordListItemDto>>>(new UserPasswordsQuery());

        Passwords.Clear();
        foreach (var pwd in queryResult.Value)
        {
            Passwords.Add(pwd);
        }

        HasPasswords = Passwords.Any();
    }

    private async void OnCopyPasswordToClipboard(PasswordListItemDto dto)
    {
        var passwordResult = await _sender.Send(new FindUserPasswordQuery(dto.Id));

        if (!passwordResult.IsError)
        {
            DataPackage dataPackage = new();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(passwordResult.Value.Value.ToString());

            Clipboard.SetContent(dataPackage);

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(ViewResourceLoader.GetString("PasswordCopiedToClipboard")));
        }
    }

    private async void OnDeletePassword(PasswordListItemDto dto)
    {
        var result = await _sender.Send(new DeletePasswordCommand(dto.Id));

        if (!result.IsError)
        {
            Passwords.Remove(dto);
            HasPasswords = Passwords.Any();
        }
    }

    private void OnCreatePassword()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.EditPassword)));
    }
}
