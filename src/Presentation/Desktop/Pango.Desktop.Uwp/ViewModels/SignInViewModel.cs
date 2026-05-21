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
using Serilog;
using System.Collections.ObjectModel;

namespace Pango.Desktop.Uwp.ViewModels;

/// <summary>
/// View model for the sign-in flow, handling user selection, authentication, and navigation steps.
/// </summary>
[AppView(AppView.SignIn)]
public partial class SignInViewModel : ViewModelBase
{
    #region Fields

    private PangoUserDto? _selectedUser;
    private readonly ISender _sender;
    private int _signInStepIndex;
    private bool _hasUsers;
    private string? _passcode;
    private bool _isCapLockWarningShown;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="SignInViewModel"/> class.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands and queries.</param>
    /// <param name="logger">Logger instance for this view model.</param>
    public SignInViewModel(ISender sender, ILogger<SignInViewModel> logger) : base(logger)
    {
        _sender = sender;

        Users = [];
        UserSelected += SignInViewModel_UserSelected;

        SignInCommand = new AsyncRelayCommand(OnSignInAsync);
        NavigateToStepCommand = new RelayCommand<int>(OnNavigateToStep);

        Log.Logger?.Debug("SignInViewModel initialized");
    }

    #region Events

    /// <summary>
    /// Event raised when a user is selected from the list.
    /// </summary>
    public event Action<PangoUserDto>? UserSelected;

    #endregion

    #region Commands

    /// <summary>
    /// Command for navigating between sign-in steps by index.
    /// </summary>
    public RelayCommand<int> NavigateToStepCommand { get; }

    /// <summary>
    /// Command for executing the sign-in process asynchronously.
    /// </summary>
    public AsyncRelayCommand SignInCommand { get; }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of available users for selection.
    /// </summary>
    public ObservableCollection<PangoUserDto> Users { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether Caps Lock warning should be shown.
    /// </summary>
    public bool IsCapLockWarningShown
    {
        get => _isCapLockWarningShown;
        set => SetProperty(ref _isCapLockWarningShown, value);
    }

    /// <summary>
    /// Gets or sets the currently selected user.
    /// </summary>
    public PangoUserDto? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value) && value is not null)
            {
                UserSelected?.Invoke(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the entered passcode for authentication.
    /// </summary>
    public string? Passcode
    {
        get => _passcode;
        set => SetProperty(ref _passcode, value);
    }

    /// <summary>
    /// Gets or sets the current step index in the sign-in wizard.
    /// </summary>
    public int SignInStepIndex
    {
        get => _signInStepIndex;
        set => SetProperty(ref _signInStepIndex, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether any users exist in the system.
    /// </summary>
    public bool HasUsers
    {
        get => _hasUsers;
        set => SetProperty(ref _hasUsers, value);
    }

    #endregion

    #region Overrides

    /// <summary>
    /// Called when the view is navigated to: loads users and restores session state.
    /// </summary>
    /// <param name="parameter">Optional navigation parameter.</param>
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        Log.Logger?.Debug("SignInViewModel navigated to");

        await base.OnNavigatedToAsync(parameter);
        await LoadUsersAsync();

        var currentUser = SecureUserSession.GetUser();
        if (currentUser is null)
        {
            Log.Logger?.Debug("No active session: navigating to user selection");
            GoToUserSelection();
        }
        else
        {
            var previouslySelectedUser = await _sender.Send<ErrorOr<PangoUserDto>>(
                new FindUserQuery(currentUser.UserName));

            SecureUserSession.ClearUser();
            App.Current.RaiseSignedOut();

            if (previouslySelectedUser.IsError)
            {
                Log.Logger?.Warning("Failed to find previously selected user: {Error}", previouslySelectedUser.FirstError.Description);
                GoToUserSelection();
                return;
            }

            SelectedUser = previouslySelectedUser.Value;
            Log.Logger?.Information("Restored session for user: {UserName}", currentUser.UserName);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles navigation to a specific sign-in step by enum index.
    /// </summary>
    /// <param name="stepIndex">The integer representation of the target SignInStep.</param>
    private void OnNavigateToStep(int stepIndex)
    {
        var step = (SignInStep)stepIndex;
        Log.Logger?.Information("Navigating to sign-in step: {Step}", step);

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
            default:
                Log.Logger?.Error("Unknown sign-in step index: {StepIndex}", stepIndex);
                throw new InvalidCastException($"Unknown sign-in step: {step}");
        }
    }

    /// <summary>
    /// Handles user selection event: navigates to passcode entry form.
    /// </summary>
    /// <param name="user">The selected user DTO.</param>
    private void SignInViewModel_UserSelected(PangoUserDto user)
    {
        Log.Logger?.Debug("User selected: {UserName}", user.UserName);
        GoToCodeEnteringForm();
    }

    /// <summary>
    /// Resets state and navigates to the user selection step.
    /// </summary>
    private void GoToUserSelection()
    {
        SelectedUser = null;
        SignInStepIndex = (int)SignInStep.SelectUser;
        Log.Logger?.Debug("Navigated to user selection step");
    }

    /// <summary>
    /// Navigates to the user creation view via messenger.
    /// </summary>
    private void GoToUserCreation()
    {
        SignInStepIndex = (int)SignInStep.CreateUser;
        WeakReferenceMessenger.Default.Send(
            new NavigationRequestedMessage(
                new NavigationParameters(AppView.EditUser, AppView.SignIn)));
        Log.Logger?.Debug("Navigation requested to EditUser view");
    }

    /// <summary>
    /// Clears passcode and navigates to the master code entry step.
    /// </summary>
    private void GoToCodeEnteringForm()
    {
        Passcode = string.Empty;
        SignInStepIndex = (int)SignInStep.EnterMastercode;
        Log.Logger?.Debug("Navigated to passcode entry step");
    }

    /// <summary>
    /// Loads the list of available users from the application layer asynchronously.
    /// </summary>
    private async Task LoadUsersAsync()
    {
        Log.Logger?.Debug("Loading users list...");

        await DoAsync(async () =>
        {
            var queryResult = await _sender.Send<ErrorOr<IEnumerable<PangoUserDto>>>(new ListQuery());
            HasUsers = !queryResult.IsError && queryResult.Value.Any();

            var userCount = queryResult.Value?.Count() ?? 0;
            Log.Logger?.Debug("Loaded {UserCount} users", userCount);

            if (!HasUsers)
            {
                Log.Logger?.Information("No users found in system");
                return;
            }

            App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                Users.Clear();
                foreach (var user in queryResult.Value!)
                {
                    Users.Add(user);
                }
                Log.Logger?.Debug("Users collection updated on UI thread");
            });
        });
    }

    /// <summary>
    /// Executes the sign-in process: validates input, authenticates user, and updates session state.
    /// </summary>
    private async Task OnSignInAsync()
    {
        if (string.IsNullOrEmpty(SelectedUser?.UserName) || string.IsNullOrEmpty(Passcode))
        {
            Log.Logger?.Debug("Sign-in attempt failed: empty username or passcode");
            WeakReferenceMessenger.Default.Send(
                new InAppNotificationMessage(
                    string.Format(ViewResourceLoader.GetString("Message_LoginEmpty"), SelectedUser?.UserName ?? string.Empty),
                    AppNotificationType.Warning));
            return;
        }

        Log.Logger?.Debug("Attempting sign-in for user: {UserName}", SelectedUser.UserName);

        var authResult = await _sender.Send(new SignInCommand(SelectedUser.UserName, Passcode));

        if (authResult.IsError)
        {
            Log.Logger?.Warning("Sign-in error for user {UserName}: {Code} - {Description}",
                SelectedUser.UserName,
                authResult.FirstError.Code,
                authResult.FirstError.Description);

            WeakReferenceMessenger.Default.Send(
                new InAppNotificationMessage(
                    string.Format(
                        ViewResourceLoader.GetString("Message_LoginError"),
                        SelectedUser.UserName,
                        authResult.FirstError.Code,
                        authResult.FirstError.Description),
                    AppNotificationType.Error));
            return;
        }

        if (!authResult.Value)
        {
            Log.Logger?.Warning("Sign-in failed for user {UserName}: invalid credentials", SelectedUser.UserName);
            WeakReferenceMessenger.Default.Send(
                new InAppNotificationMessage(
                    string.Format(ViewResourceLoader.GetString("Message_LoginFailed"), SelectedUser.UserName),
                    AppNotificationType.Warning));
            return;
        }

        SecureUserSession.SaveUser(SelectedUser.UserName);
        App.Current.RaiseLoginSucceeded(SelectedUser.UserName);
        WeakReferenceMessenger.Default.Send(
            new InAppNotificationMessage(
                ViewResourceLoader.GetString("Message_LoginSuccess"),
                AppNotificationType.Success));

        Log.Logger?.Information("User {UserName} successfully signed in", SelectedUser.UserName);
    }

    #endregion
}
