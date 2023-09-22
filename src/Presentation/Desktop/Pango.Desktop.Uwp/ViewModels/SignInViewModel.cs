using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErrorOr;
using MediatR;
using Pango.Application.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

public class SignInViewModel : ObservableObject, IViewModel
{
    #region Fields

    private string _selectedUser;
    private ISender _sender;

    #endregion

    public SignInViewModel(ISender sender)
    {
        _sender = sender;

        Users = new();
        SignInCommand = new AsyncRelayCommand(OnSignIn);
    }

    #region Events

    public event Action SignInSuceeded;

    #endregion

    #region Commands

    public AsyncRelayCommand SignInCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<UserDto> Users { get; private set; }

    public string SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
    }

    #endregion

    #region Overrides

    public async Task OnNavigatedFromAsync(object parameter)
    {
        await Task.CompletedTask;
    }

    public async Task OnNavigatedToAsync(object parameter)
    {
        await LoadUsersAsync();
    }

    #endregion

    #region Private Methods

    private async Task LoadUsersAsync()
    {
        var queryResult = await _sender.Send<ErrorOr<IEnumerable<UserDto>>>(new ListQuery());

        Users.Clear();
        foreach (var user in queryResult.Value)
        {
            Users.Add(user);
        }
    }

    private async Task OnSignIn()
    {
        await Task.CompletedTask;

        SignInSuceeded?.Invoke();
    }

    #endregion
}
