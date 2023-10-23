using CommunityToolkit.Mvvm.Input;
using ErrorOr;
using MediatR;
using Pango.Application.Models;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.SignIn)]
public class SignInViewModel : ViewModelBase
{
    #region Fields

    private UserDto _selectedUser;
    private ISender _sender;
    private int _signInStepIndex;

    #endregion

    public SignInViewModel(ISender sender)
    {
        _sender = sender;

        Users = new();
        UserSelected += SignInViewModel_UserSelected;

        SignInCommand = new AsyncRelayCommand(OnSignIn);
        NavigateToStep = new RelayCommand<int>(OnNavigateToStep);
    }

    #region Events

    public event Action<string> SignInSuceeded;
    public event Action<UserDto> UserSelected;

    #endregion

    #region Commands

    public RelayCommand<int> NavigateToStep { get; }

    public AsyncRelayCommand SignInCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<UserDto> Users { get; private set; }

    public UserDto SelectedUser
    {
        get => _selectedUser;
        set
        {
            SetProperty(ref _selectedUser, value);
            UserSelected?.Invoke(value);
        }
    }

    public int SignInStepIndex
    {
        get => _signInStepIndex;
        set => SetProperty(ref _signInStepIndex, value);
    }

    #endregion

    #region Overrides

    public override async Task OnNavigatedToAsync(object parameter)
    {
        GoToUserSelection();
        await LoadUsersAsync();
    }

    #endregion

    #region Private Methods

    private void OnNavigateToStep(int stepIndex)
    {
        SignInStep step = (SignInStep)stepIndex;

        switch (step)
        {
            case SignInStep.SelectUser:
                GoToUserSelection();
                break;
            case SignInStep.CreateUser:
                GoToUserCreation();
                break;
            case SignInStep.EnterMastercode:
                GoToCodeEnteringForm();
                break;
            default: throw new InvalidCastException("Unkown step");
        };
    }

    private void SignInViewModel_UserSelected(UserDto obj)
    {
        GoToCodeEnteringForm();
    }

    private void GoToUserSelection()
    {
        SignInStepIndex = (int)SignInStep.SelectUser;
    }

    private void GoToUserCreation()
    {
        SignInStepIndex = (int)SignInStep.CreateUser;
    }

    private void GoToCodeEnteringForm()
    {
        SignInStepIndex = (int)SignInStep.EnterMastercode;
    }

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

        SignInSuceeded?.Invoke(SelectedUser.UserName);
    }

    #endregion
}
