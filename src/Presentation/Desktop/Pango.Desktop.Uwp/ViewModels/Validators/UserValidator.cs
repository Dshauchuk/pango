using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.ViewModels.Validators;

public class UserValidator : ObservableValidator
{
    private string _passwordConfirmation;
    private string _password;
    private string _userName;
    private static ResourceLoader _viewResourceLoader = new ResourceLoader();

    public UserValidator()
    {
    }

    [Required()]
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    [Required()]
    public string Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value);
            OnPropertyChanged(nameof(Password));
            OnPropertyChanged(nameof(PasswordConfirmation));
        }
    }

    [Required()]
    [CustomValidation(typeof(UserValidator), nameof(ValidatePassword))]
    public string PasswordConfirmation
    {
        get => _passwordConfirmation;
        set => SetProperty(ref _passwordConfirmation, value);
    }

    public void Validate()
    {
        ValidateAllProperties();
    }

    public static ValidationResult ValidatePassword(string confirmation, ValidationContext context)
    {
        UserValidator validator = (UserValidator)context.ObjectInstance;
        bool isValid = validator.Password == confirmation;

        if (isValid)
        {
            return ValidationResult.Success;
        }

        return new(_viewResourceLoader.GetString("PasswordConfirmationDoesNotMatch"));
    }

}
