using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using Pango.Application.UseCases.User.Commands.Delete;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Dialogs;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.User)]
public class UserViewModel : ViewModelBase
{
    private readonly ISender _sender;
    private readonly IUserContextProvider _userContext;
    private readonly ILogger<UserViewModel> _logger;
    private readonly IDialogService _dialogService;
    private readonly IUserStorageManager _storageManager;
    private string _currentUserName = string.Empty;
    private string _userContentInfo = "...";

    public UserViewModel(ISender sender, IUserContextProvider userContext, ILogger<UserViewModel> logger, IDialogService dialogService, IUserStorageManager storageManager) : base(logger)
    {
        _sender = sender;
        _userContext = userContext;
        _logger = logger;
        _dialogService = dialogService;
        _storageManager = storageManager;

        DeleteUserCommand = new(OnDeleteUser);
        SignOutCommand = new(OnSignOut);
        OpenChangePasswordDialogCommand = new(OnOpenChangePasswordDialog);
    }

    #region Commands

    public RelayCommand DeleteUserCommand { get; }
    public RelayCommand SignOutCommand { get; }
    public RelayCommand OpenChangePasswordDialogCommand { get; }

    #endregion

    #region Properties

    public string CurrentUserName
    {
        get => _currentUserName;
        set => SetProperty(ref _currentUserName, value);
    }

    public string UserContentInfo
    {
        get => _userContentInfo;
        set => SetProperty(ref _userContentInfo, value);
    }

    #endregion

    #region Overrides

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        CurrentUserName = _userContext.GetUserName();

        var queryResult = await _sender.Send<ErrorOr<IEnumerable<PangoPasswordListItemDto>>>(new UserPasswordsQuery());

        if (!queryResult.IsError)
        {
            UserContentInfo = string.Format(ViewResourceLoader.GetString("Content_FormattedValueLabel"), queryResult.Value.Count(p => !p.IsCatalog));
        }
    }

    #endregion

    private void OnOpenChangePasswordDialog()
    {
        _dialogService.ShowPasswordChangeDialogAsync(new EmptyDialogParameter()); 
    }

    private void OnSignOut()
    {
        _logger.LogDebug("User \"{currentUserName}\" logged out", _currentUserName);
        SecureUserSession.ClearUser();
        App.Current.RaiseSignedOut();
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new(new NavigationParameters(AppView.SignIn, AppView.User)));
    }

    private async void OnDeleteUser()
    {
        string confirmationTitle = ViewResourceLoader.GetString("DeleteUser");
        string confirmationDescription = string.Format(ViewResourceLoader.GetString("Confirm_UserDeletion"), _currentUserName);

        bool deletionConfirmed = await _dialogService.ConfirmAsync(confirmationTitle, confirmationDescription);

        if(!deletionConfirmed)
        {
            return;
        }

        ErrorOr<bool> result = await _sender.Send(new DeleteUserCommand(_currentUserName));

        if (!result.IsError && result.Value)
        {
            _logger.LogDebug("User \"{currentUserName}\" successfully deleted", _currentUserName);
        }
        else
        {
            _logger.LogWarning("User deletion failed: {FirstError}", result.FirstError);
        }
        OnSignOut();
    }
}