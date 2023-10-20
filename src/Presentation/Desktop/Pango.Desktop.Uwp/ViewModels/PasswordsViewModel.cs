using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using Pango.Desktop.Uwp.Mvvm.Messages;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

public sealed class PasswordsViewModel : ObservableObject, IViewModel
{
    private ISender _sender;
    private bool _hasPasswords;

    public PasswordsViewModel(ISender sender)
    {
        _sender = sender;
        Passwords = new();

        CreatePasswordCommand = new RelayCommand(OnCreatePassword);
    }

    #region Commands

    public RelayCommand CreatePasswordCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<PasswordDto> Passwords { get; private set; }

    public bool HasPasswords
    {
        get => _hasPasswords;
        set => SetProperty(ref _hasPasswords, value);
    }

    #endregion

    public async Task OnNavigatedFromAsync(object parameter)
    {
        await Task.CompletedTask;
    }

    public async Task OnNavigatedToAsync(object parameter)
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

    private void OnCreatePassword()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(Core.Enums.AppView.NewPassword));
    }
}
