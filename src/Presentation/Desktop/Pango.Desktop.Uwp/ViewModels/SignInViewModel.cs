using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

public class SignInViewModel : ObservableObject, IViewModel
{
    #region Fields

    private string _selectedUser;

    #endregion

    public SignInViewModel()
    {
        Users = new() { "John Doe", "Jane Doe" };

        SignInCommand = new AsyncRelayCommand(OnSignIn);
    }

    #region Events

    public event Action SignInSuceeded;

    #endregion

    #region Commands

    public AsyncRelayCommand SignInCommand { get; }

    #endregion

    #region Properties

    public List<string> Users { get; }

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
        await Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private async Task OnSignIn()
    {
        await Task.CompletedTask;

        SignInSuceeded?.Invoke();
    }

    #endregion
}
