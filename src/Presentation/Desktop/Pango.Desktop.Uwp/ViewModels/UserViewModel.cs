using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.User.Commands.Delete;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using System;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.User)]
public class UserViewModel : ViewModelBase
{
    private readonly ISender _sender;
    private readonly IUserContextProvider _userContext;
    private readonly ILogger<UserViewModel> _logger;
    private string _currentUserName;

    public UserViewModel(ISender sender, IUserContextProvider userContext, ILogger<UserViewModel> logger) : base(logger)
    {
        _sender = sender;
        _userContext = userContext;
        _logger = logger;

        DeleteUserCommand = new(OnDeleteUser);
    }

    #region Commands

    public RelayCommand DeleteUserCommand { get; }

    #endregion

    #region Properties

    public string CurrentUserName
    {
        get => _currentUserName;
        set => SetProperty(ref _currentUserName, value);
    }

    #endregion

    public override Task OnNavigatedToAsync(object parameter)
    {
        CurrentUserName = _userContext.GetUserName();

        return Task.CompletedTask;
    }

    private async void OnDeleteUser()
    {
        ErrorOr<bool> result = await _sender.Send(new DeleteUserCommand(_currentUserName));

        if (!result.IsError && result.Value)
        {
            _logger.LogDebug($"User \"{_currentUserName}\" successfully deleted");
            WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(AppView.SignIn)));
        }
        else
        {
            _logger.LogWarning($"User deletion failed: {result.FirstError}");
        }
    }
}