using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.DeletePassword;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.PasswordsIndex)]
public sealed class PasswordsViewModel : ViewModelBase
{
    private ISender _sender;
    private bool _hasPasswords;

    public PasswordsViewModel(ISender sender)
    {
        _sender = sender;
        Passwords = new();

        CreatePasswordCommand = new RelayCommand(OnCreatePassword);
        DeletePasswordCommand = new RelayCommand<PasswordDto>(OnDeletePassword);
    }

    #region Commands

    public RelayCommand CreatePasswordCommand { get; }
    public RelayCommand<PasswordDto> DeletePasswordCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<PasswordDto> Passwords { get; private set; }

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
        var queryResult = await _sender.Send<ErrorOr<IEnumerable<PasswordDto>>>(new UserPasswordsQuery());

        Passwords.Clear();
        foreach (var pwd in queryResult.Value)
        {
            Passwords.Add(pwd);
        }

        HasPasswords = Passwords.Any();
    }

    private async void OnDeletePassword(PasswordDto dto)
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
