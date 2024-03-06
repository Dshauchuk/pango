using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
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

    private string _currentUserName;

    public UserViewModel(ISender sender, IUserContextProvider userContext)
    {
        _sender = sender;
        _userContext = userContext;
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
            WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(AppView.SignIn)));
        }
    }
}