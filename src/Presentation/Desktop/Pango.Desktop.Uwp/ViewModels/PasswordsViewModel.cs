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
        EditPasswordCommand = new RelayCommand<PangoPasswordListItemDto>(OnEditPassword);
        DeletePasswordCommand = new RelayCommand<PangoPasswordListItemDto>(OnDeletePassword);
        CopyPasswordToClipboardCommand = new RelayCommand<PangoPasswordListItemDto>(OnCopyPasswordToClipboard);
    }

    #region Commands

    public RelayCommand CreatePasswordCommand { get; }
    public RelayCommand<PangoPasswordListItemDto> EditPasswordCommand { get; }
    public RelayCommand<PangoPasswordListItemDto> DeletePasswordCommand { get; }
    public RelayCommand<PangoPasswordListItemDto> CopyPasswordToClipboardCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<PangoPasswordListItemDto> Passwords { get; private set; }

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

        var passwordsOrderedList = queryResult.Value.OrderBy(p => p.Name);

        Passwords.Clear();
        foreach (var pwd in passwordsOrderedList)
        {
            Passwords.Add(pwd);
        }

        HasPasswords = Passwords.Any();
    }

    private async void OnCopyPasswordToClipboard(PangoPasswordListItemDto dto)
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

    private async void OnDeletePassword(PangoPasswordListItemDto dto)
    {
        var result = await _sender.Send(new DeletePasswordCommand(dto.Id));

        if (!result.IsError)
        {
            Passwords.Remove(dto);
            HasPasswords = Passwords.Any();
        }
    }

    private void OnEditPassword(PangoPasswordListItemDto selected)
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.EditPassword, selected?.Id)));
    }

    private void OnCreatePassword()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.EditPassword)));
    }
}
