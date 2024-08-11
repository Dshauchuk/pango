using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.User.Commands.ChangePassword;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.Security;
using Pango.Desktop.Uwp.ViewModels;
using Pango.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public class ChangePasswordValidator : ObservableValidator
{
    #region Fields

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmation = string.Empty;
    private static readonly ResourceLoader _viewResourceLoader = new();

    #endregion

    #region Properties

    public Action? OnDataChanged;

    [Required()]
    public string CurrentPassword
    {
        get => _currentPassword;
        set
        {
            SetProperty(ref _currentPassword, value);
            OnDataChanged?.Invoke();
        }
    }

    [Required()]
    public string NewPassword
    {
        get => _newPassword;
        set
        {
            SetProperty(ref _newPassword, value);
            OnPropertyChanged(nameof(NewPassword));
            OnPropertyChanged(nameof(Confirmation));
            OnDataChanged?.Invoke();
        }
    }

    [Required()]
    [CustomValidation(typeof(ChangePasswordValidator), nameof(ValidatePassword))]
    public string Confirmation
    {
        get => _confirmation;
        set
        {
            SetProperty(ref _confirmation, value);
            OnDataChanged?.Invoke();
        }
    }

    #endregion

    public void Validate()
    {
        ValidateAllProperties();
    }

    public static ValidationResult? ValidatePassword(string confirmation, ValidationContext context)
    {
        ChangePasswordValidator validator = (ChangePasswordValidator)context.ObjectInstance;
        bool isValid = validator.NewPassword == confirmation;

        if (isValid)
        {
            return ValidationResult.Success;
        }

        return new(_viewResourceLoader.GetString("PasswordConfirmationDoesNotMatch"));
    }
}

public class ChangePasswordDialogViewModel : ViewModelBase, IDialogViewModel
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
        Validator.Validate();

        return !Validator.HasErrors;
    }

    public Task OnCancelAsync()
    {
        return Task.CompletedTask;
    }

    public async Task OnSaveAsync()
    {
        Validator.Validate();

        if (!Validator.HasErrors)
        {
            string currentUser = _userContextProvider.GetUserName();

            PangoUser? user = await _userRepository.FindAsync(currentUser);

            if (user is null)
                return;

            if(!_passwordHashProvider.VerifyPassword(Validator.CurrentPassword, user.MasterPasswordHash, Convert.FromBase64String(user.PasswordSalt)))
            {
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage("Password is not correct", Core.Enums.AppNotificationType.Warning));
                return;
            }

            ErrorOr<bool> result = await _sender.Send(new ChangePasswordCommand(_userContextProvider.GetUserName(), Validator.NewPassword, Guid.NewGuid().ToString("N")));

            if (!result.IsError)
            {
                SecureUserSession.ClearUser();
                App.Current.RaiseSignedOut();
                WeakReferenceMessenger.Default.Send<NavigationRequstedMessage>(new(new NavigationParameters(AppView.SignIn, AppView.User)));
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
