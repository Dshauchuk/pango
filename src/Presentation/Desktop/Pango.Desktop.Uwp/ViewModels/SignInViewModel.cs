using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Models;
using Pango.Application.UseCases.User.Commands.SignIn;
using Pango.Application.UseCases.User.Queries.FindUser;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.Security;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.SignIn)]
public class SignInViewModel : ViewModelBase
{
    #region Fields

    private PangoUserDto? _selectedUser;
    private readonly ISender _sender;
    private int _signInStepIndex;
    private bool _hasUsers;
    private string? _passcode;
    private bool _isCapLockWarningShown;

    #endregion

    public SignInViewModel(ISender sender, ILogger<SignInViewModel> logger) : base(logger)
    {
        _sender = sender;

        Users = [];
        UserSelected += SignInViewModel_UserSelected;

        SignInCommand = new AsyncRelayCommand(OnSignIn);
        NavigateToStepCommand = new RelayCommand<int>(OnNavigateToStep);
    }

    #region Events

    public event Action<PangoUserDto> UserSelected;

    #endregion

    #region Commands

    public RelayCommand<int> NavigateToStepCommand { get; }

    public AsyncRelayCommand SignInCommand { get; }

    #endregion

    #region Properties

    public ObservableCollection<PangoUserDto> Users { get; private set; }

    public bool IsCapLockWarningShown
    {
        get => _isCapLockWarningShown;
        set => SetProperty(ref _isCapLockWarningShown, value);
    }

    public PangoUserDto? SelectedUser
    {
        get => _selectedUser;
        set
        {
            SetProperty(ref _selectedUser, value);

            if(value is not null)
            {
                UserSelected?.Invoke(value);
            }
        }
    }

    public string? Passcode
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

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        await LoadUsersAsync();

        var currentUser = SecureUserSession.GetUser();
        if (currentUser is null)
        {
            GoToUserSelection();
        }
        else
        {
            ErrorOr<PangoUserDto> previouslySelectedUser = await _sender.Send<ErrorOr<PangoUserDto>>(new FindUserQuery(currentUser.UserName));
            SecureUserSession.ClearUser();
            App.Current.RaiseSignedOut();
            if (previouslySelectedUser.IsError)
            {
                GoToUserSelection();

                return;
            }

            SelectedUser = previouslySelectedUser.Value;
        }
    }

    #endregion

    #region Private Methods

    private void OnNavigateToStep(int stepIndex)
    {
        SignInStep step = (SignInStep)stepIndex;

        Logger.LogInformation($"Navigating to {step}");

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
        WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.EditUser, AppView.SignIn)));
    }

    private void GoToCodeEnteringForm()
    {
        Passcode = string.Empty;
        SignInStepIndex = (int)SignInStep.EnterMastercode;
    }

    private async Task LoadUsersAsync()
    {
        Logger.LogDebug($"Loading users...");

        await DoAsync(async () =>
        {
            var queryResult = await _sender.Send<ErrorOr<IEnumerable<PangoUserDto>>>(new ListQuery());
            HasUsers = !queryResult.IsError && queryResult.Value.Any();

            Logger.LogDebug($"{queryResult.Value?.Count()} users loaded");

            if (!HasUsers)
            {
                return;
            }

            Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            dispatcherQueue.TryEnqueue(() =>
            {
                Users.Clear();
                foreach (var user in queryResult.Value!)
                {
                    Users.Add(user);
                }
            });
        });
    }

    private async Task OnSignIn()
    {
        if(string.IsNullOrEmpty(SelectedUser?.UserName) || string.IsNullOrEmpty(Passcode))
        {
            Logger.LogDebug("Login failed: empty username or passcode");
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(string.Format(ViewResourceLoader.GetString("Message_LoginEmpty"), SelectedUser?.UserName ?? string.Empty), AppNotificationType.Warning));

            return;
        }

        var auth = await _sender.Send(new SignInCommand(SelectedUser.UserName, Passcode));

        if (auth.IsError)
        {
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(string.Format(ViewResourceLoader.GetString("Message_LoginError"), SelectedUser.UserName, auth.FirstError.Code, auth.FirstError.Description), AppNotificationType.Error));
            Logger.LogDebug("Login failed for user \"{UserName}\": {Code}. {Description}", SelectedUser.UserName, auth.FirstError.Code, auth.FirstError.Description);

            return;
        }

        if (!auth.Value)
        {
            // show error
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(string.Format(ViewResourceLoader.GetString("Message_LoginFailed"), SelectedUser.UserName), AppNotificationType.Warning));
            Logger.LogDebug("Login failed for user \"{UserName}\": User name or password is wrong", SelectedUser.UserName);

            return;
        }

        SecureUserSession.SaveUser(SelectedUser.UserName);
        App.Current.RaiseLoginSucceeded(SelectedUser.UserName);
        WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(ViewResourceLoader.GetString("Message_LoginSuccess"), AppNotificationType.Success));

        Logger.LogDebug("User \"{UserName}\" successfully signed in", SelectedUser.UserName);
    }

    #endregion
}
