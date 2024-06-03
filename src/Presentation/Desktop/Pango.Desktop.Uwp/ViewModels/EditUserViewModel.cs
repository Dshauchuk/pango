using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.ViewModels.Validators;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.EditUser)]
public sealed class EditUserViewModel : ViewModelBase
{
    #region Fields

    private readonly ISender _sender;

    private UserValidator _userValidator;

    #endregion

    public EditUserViewModel(ISender sender, ILogger<EditUserViewModel> logger): base(logger)
    {
        _sender = sender;

        OpenSignInViewCommand = new RelayCommand(OnOpenSignInView);
        SaveUserComand = new RelayCommand(OnSaveUser);
    }

    #region Properties

    public UserValidator UserValidator
    {
        get => _userValidator;
        set => SetProperty(ref _userValidator, value);
    }

    #endregion

    #region Commands

    public RelayCommand OpenSignInViewCommand { get; }
    public RelayCommand SaveUserComand { get; }

    #endregion

    #region Overrides

    public override Task OnNavigatedToAsync(object parameter)
    {
        Clear();

        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private void Clear()
    {
        UserValidator = new UserValidator();
    }

    private async void OnSaveUser()
    {
        UserValidator.Validate();

        if (!UserValidator.HasErrors)
        {
            await _sender.Send(new RegisterUserCommand(UserValidator.UserName, UserValidator.Password));
            OnOpenSignInView();
        }
    }

    private void OnOpenSignInView()
    {
        WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new NavigationRequstedMessage(new Mvvm.Models.NavigationParameters(Core.Enums.AppView.SignIn)));
    }

    #endregion
}
