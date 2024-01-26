using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
using Pango.Application.Models;
using Pango.Application.UseCases.User.Commands.SignIn;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.SignIn)]
public class SignInViewModel : ViewModelBase
{
    #region Fields

    private PangoUserDto _selectedUser;
    private readonly ISender _sender;
    private int _signInStepIndex;
    private bool _hasUsers;
    private string _passcode;

    #endregion

    public SignInViewModel(ISender sender)
    {
        _sender = sender;

        Users = new();
        UserSelected += SignInViewModel_UserSelected;

        SignInCommand = new AsyncRelayCommand(OnSignIn);
        NavigateToStepCommand = new RelayCommand<int>(OnNavigateToStep);
    }

    #region Events

    public event Action<string> SignInSuceeded;
    public event Action<PangoUserDto> UserSelected;

    #endregion

    #region Commands

    public RelayCommand<int> NavigateToStepCommand { get; }

    public AsyncRelayCommand SignInCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<PangoUserDto> Users { get; private set; }

    public PangoUserDto SelectedUser
    {
        get => _selectedUser;
        set
        {
            SetProperty(ref _selectedUser, value);
            UserSelected?.Invoke(value);
        }
    }

    public string Passcode
    {
        get => _passcode;
        set => SetProperty(ref _passcode, value);
    }

    public int SignInStepIndex
    {
        get => _signInStepIndex;
        set => SetProperty(ref _signInStepIndex, value);
    }

    public bool HasUsers
    {
        get => _hasUsers;
        set => SetProperty(ref _hasUsers, value);   
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
            default: throw new InvalidCastException("Unknown step");
        };
    }

    private void SignInViewModel_UserSelected(PangoUserDto obj)
    {
        GoToCodeEnteringForm();
    }

    private void GoToUserSelection()
    {
        SelectedUser = null;
        SignInStepIndex = (int)SignInStep.SelectUser;
    }

    private void GoToUserCreation()
    {
        SignInStepIndex = (int)SignInStep.CreateUser;
        WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.EditUser)));
    }

    private void GoToCodeEnteringForm()
    {
        Passcode = string.Empty;
        SignInStepIndex = (int)SignInStep.EnterMastercode;
    }

    private async Task LoadUsersAsync()
    {
        await DoAsync(async () =>
        {
            var queryResult = await _sender.Send<ErrorOr<IEnumerable<PangoUserDto>>>(new ListQuery());
            HasUsers = !queryResult.IsError && queryResult.Value.Any();

            if (!HasUsers)
            {
                return;
            }

            DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            dispatcherQueue.TryEnqueue(() =>
            {
                Users.Clear();
                foreach (var user in queryResult.Value)
                {
                    Users.Add(user);
                }
            });
        });
    }

    private async Task OnSignIn()
    {
        var auth = await _sender.Send(new SignInCommand(SelectedUser.UserName, Passcode));

        if (auth.IsError)
        {
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage($"{auth.FirstError.Code}. {auth.FirstError.Description}", AppNotificationType.Error));

            return;
        }

        if (!auth.Value)
        {
            // show error
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage("User name or password is wrong", AppNotificationType.Error));

            return;
        }

        SignInSuceeded?.Invoke(SelectedUser.UserName);
    }

    #endregion
}
