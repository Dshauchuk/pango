using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.User.Commands.ChangePassword;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Dialogs.Validators;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.Security;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Domain.Entities;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public partial class ChangePasswordDialogViewModel : ViewModelBase, IDialogViewModel
{
    #region Fields

    private readonly ISender _sender;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashProvider _passwordHashProvider;

    private ChangePasswordValidator _validator;
    #endregion

    public ChangePasswordDialogViewModel(
        ISender sender,
        IUserContextProvider userContextProvider,
        IPasswordHashProvider passwordHashProvider,
        IUserRepository userRepository,
        ILogger<ChangePasswordDialogViewModel> logger)
        : base(logger)
    {
        _sender = sender;

        DialogContext = new DialogContext();

        _validator = CreateValidator();
        _userContextProvider = userContextProvider;
        _passwordHashProvider = passwordHashProvider;
        _userRepository = userRepository;
    }

    #region Properties

    public IDialogContext DialogContext { get; }

    public ChangePasswordValidator Validator
    {
        get => _validator;
        set => SetProperty(ref _validator, value);
    }

    #endregion

    #region Override

    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        Validator = CreateValidator();
    }

    #endregion

    #region IDialogViewModel

    public bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Validator.CurrentPassword) &&
               !string.IsNullOrWhiteSpace(Validator.NewPassword) &&
               !string.IsNullOrWhiteSpace(Validator.Confirmation) &&
               Validator.NewPassword == Validator.Confirmation;
    }

    public Task OnCancelAsync()
    {
        return Task.CompletedTask;
    }

    public async Task OnSaveAsync()
    {
        Logger.LogInformation("Attempting to change user password.");
        ChangePasswordValidator.Validate();

        if (!Validator.HasErrors)
        {
            string currentUser = _userContextProvider.GetUserName();
            PangoUser? user = await _userRepository.FindAsync(currentUser);

            if (user is null)
            {
                Logger.LogWarning("Change password failed: User {User} not found.", currentUser);
                return;
            }

            if (!_passwordHashProvider.VerifyPassword(Validator.CurrentPassword, user.MasterPasswordHash, Convert.FromBase64String(user.PasswordSalt)))
            {
                Logger.LogWarning("Change password failed: Invalid current password provided.");
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(ViewResourceLoader.GetString("PasswordIsNotCorrect"), AppNotificationType.Warning));
                return;
            }

            ErrorOr<bool> result = await _sender.Send(new ChangePasswordCommand(_userContextProvider.GetUserName(), Validator.NewPassword, Guid.NewGuid().ToString("N")));

            if (result.IsError)
            {
                Logger.LogError("Password change command returned an error.");
                string message = !string.IsNullOrWhiteSpace(result.FirstError.Description)
                    ? result.FirstError.Description
                    : ViewResourceLoader.GetString("PasswordIsNotCorrect");
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(message, AppNotificationType.Warning));
            }
            else
            {
                Logger.LogInformation("Password successfully changed. Clearing session and logging out.");
                SecureUserSession.ClearUser();

                var passwordRepo = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IPasswordRepository>(App.Host.Services);
                passwordRepo.ClearCache();

                App.Current.RaiseSignedOut();
                WeakReferenceMessenger.Default.Send<NavigationRequestedMessage>(new(new NavigationParameters(AppView.SignIn, AppView.User)));
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(ViewResourceLoader.GetString("PasswordHasBeenChanged"), AppNotificationType.Success));
            }
        }
    }

    #endregion

    #region Private Methods

    private ChangePasswordValidator CreateValidator()
    {
        return new()
        {
            OnDataChanged = () => DialogContext.RaiseDialogContentChanged()
        };
    }

    #endregion
}
