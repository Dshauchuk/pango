using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel.DataAnnotations;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.Dialogs.Validators;

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
