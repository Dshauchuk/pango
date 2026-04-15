using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Application.UseCases.Password.Commands.GeneratePassword;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Dialogs.Parameters;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

public partial class GeneratePasswordDialogViewModel : ViewModelBase, IDialogViewModel
{
    #region Fields
    private readonly ISender _sender;
    private readonly IPasswordGeneratorSettingsService _settingsService;
    private string _generatedPassword = string.Empty;
    private int _length = PasswordConstants.SafeLength;
    private string _lengthError = string.Empty;
    private string _charsetsError = string.Empty;
    private bool _useUppercase = true;
    private bool _useLowercase = true;
    private bool _useDigits = true;
    private bool _useSpecial = false;
    private bool _excludeAmbiguous = false;
    private double _strengthBarWidth;
    private PasswordStrength _strength;
    #endregion

    #region Properties
    public string GeneratedPassword
    {
        get => _generatedPassword;
        private set
        {
            if (_generatedPassword == value)
                return;

            _generatedPassword = value;
            OnPropertyChanged(nameof(GeneratedPassword));
            UpdateStrength();
            CopyPasswordCommand.NotifyCanExecuteChanged();
        }
    }

    public int Length
    {
        get => _length;
        set
        {
            if (_length == value)
                return;

            _length = value;
            OnPropertyChanged(nameof(Length));
            ValidateLength(value);
        }
    }
    public string LengthError
    {
        get => _lengthError;
        set
        {
            if (_lengthError == value)
                return;

            _lengthError = value;
            OnPropertyChanged(nameof(LengthError));
        }
    }
    public string CharsetsError
    {
        get => _charsetsError;
        set
        {
            if (_charsetsError == value)
                return;

            _charsetsError = value;
            OnPropertyChanged(nameof(CharsetsError));
        }
    }


    public bool UseUppercase
    {
        get => _useUppercase;
        set
        {
            if (_useUppercase == value)
                return;

            _useUppercase = value;
            OnPropertyChanged(nameof(UseUppercase));
        }
    }

    public bool UseLowercase
    {
        get => _useLowercase;
        set
        {
            if (_useLowercase == value)
                return;

            _useLowercase = value;
            OnPropertyChanged(nameof(UseLowercase));
        }
    }

    public bool UseDigits
    {
        get => _useDigits;
        set
        {
            if (_useDigits == value)
                return;

            _useDigits = value;
            OnPropertyChanged(nameof(UseDigits));
        }
    }

    public bool UseSpecial
    {
        get => _useSpecial;
        set
        {
            if (_useSpecial == value)
                return;

            _useSpecial = value;
            OnPropertyChanged(nameof(UseSpecial));
        }
    }

    public bool ExcludeAmbiguous
    {
        get => _excludeAmbiguous;
        set
        {
            if (_excludeAmbiguous == value)
                return;

            _excludeAmbiguous = value;
            OnPropertyChanged(nameof(ExcludeAmbiguous));
        }
    }

    public PasswordStrength Strength
    {
        get => _strength;
        private set
        {
            if (_strength == value)
                return;

            _strength = value;
            OnPropertyChanged(nameof(Strength));
            OnPropertyChanged(nameof(StrengthLabel));
            OnPropertyChanged(nameof(StrengthBrush));
        }
    }

    public string StrengthLabel =>
        Strength switch
        {
            PasswordStrength.Weak => ViewResourceLoader.GetString("PasswordStrength_Weak"),
            PasswordStrength.Medium => ViewResourceLoader.GetString("PasswordStrength_Medium"),
            PasswordStrength.Strong => ViewResourceLoader.GetString("PasswordStrength_Strong"),
            _ => string.Empty
        };

    public SolidColorBrush StrengthBrush =>
        Strength switch
        {
            PasswordStrength.Weak => new SolidColorBrush(Colors.Red),
            PasswordStrength.Medium => new SolidColorBrush(Colors.Orange),
            PasswordStrength.Strong => new SolidColorBrush(Colors.Green),
            _ => new SolidColorBrush(Colors.Gray)
        };
    public double StrengthBarWidth
    {
        get => _strengthBarWidth;
        set => SetProperty(ref _strengthBarWidth, value);
    }
    public IDialogContext DialogContext { get; }


    #endregion

    #region Commands

    public IAsyncRelayCommand GenerateCommand { get; }
    public IAsyncRelayCommand RegeneratePasswordCommand { get; }
    public RelayCommand CopyPasswordCommand { get; }

    #endregion

    public GeneratePasswordDialogViewModel(ISender sender, ILogger<GeneratePasswordDialogViewModel> logger, IPasswordGeneratorSettingsService settingsService)
        : base(logger)
    {
        _sender = sender;
        _settingsService = settingsService;

        GenerateCommand = new AsyncRelayCommand(GenerateAsync);
        RegeneratePasswordCommand = new AsyncRelayCommand(GenerateAsync);
        CopyPasswordCommand = new RelayCommand(CopyPassword, CanCopyPassword);
        DialogContext = new DialogContext();

        WeakReferenceMessenger.Default.Register<UserSignedOutMessage>(this, (_, _) =>
        {
            Clear();
        });
    }

    #region Overrides
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        await base.OnNavigatedToAsync(parameter);

        var s = _settingsService.Load();

        Length = s.Length;
        UseUppercase = s.UseUppercase;
        UseLowercase = s.UseLowercase;
        UseDigits = s.UseDigits;
        UseSpecial = s.UseSpecial;
        ExcludeAmbiguous = s.ExcludeAmbiguous;

        if (parameter is GeneratePasswordDialogParameters p
            && !string.IsNullOrEmpty(p.GeneratedPassword))
        {
            GeneratedPassword = p.GeneratedPassword;
        }

        DialogContext.RaiseDialogContentChanged();
    }
    #endregion

    private void SaveSettings()
    {
        var s = new PasswordGeneratorSettings
        {
            Length = Length,
            UseUppercase = UseUppercase,
            UseLowercase = UseLowercase,
            UseDigits = UseDigits,
            UseSpecial = UseSpecial,
            ExcludeAmbiguous = ExcludeAmbiguous
        };

        _settingsService.Save(s);
    }
    private async Task GenerateAsync()
    {
        if (!ValidateLength(Length))
            return;
        if (!ValidateCharsets())
            return;
        var command = new GeneratePasswordCommand(
            Length,
            UseUppercase,
            UseLowercase,
            UseDigits,
            UseSpecial,
            ExcludeAmbiguous);

        var result = await _sender.Send(command);

        if (result.IsError)
        {
            Logger.LogError("Password generation failed: {Errors}", string.Join(", ", result.Errors));

            var message = result.FirstError.Description
                          ?? ViewResourceLoader.GetString("PasswordGenerationFailed");

            WeakReferenceMessenger.Default.Send(
                new InAppNotificationMessage(message, AppNotificationType.Error));

            return;
        }

        GeneratedPassword = result.Value;

        SaveSettings();
        DialogContext.RaiseDialogContentChanged();

        WeakReferenceMessenger.Default.Send(
            new InAppNotificationMessage(
                ViewResourceLoader.GetString("PasswordGeneratedSuccessfully")));
    }
    private bool ValidateLength(int value)
    {
        if (value < PasswordConstants.MinLength || value > PasswordConstants.MaxLength)
        {
            LengthError = ViewResourceLoader.GetString("PasswordLength_OutOfRange");
            return false;
        }
        LengthError = string.Empty;
        return true;
    }

    private bool ValidateCharsets()
    {
        if (!UseUppercase && !UseLowercase && !UseDigits && !UseSpecial)
        {
            CharsetsError = ViewResourceLoader.GetString("PasswordGeneration_Error_NoCharSet");
            return false;
        }

        CharsetsError = string.Empty;
        return true;
    }
    private bool CanCopyPassword() => !string.IsNullOrEmpty(GeneratedPassword);
    private void CopyPassword()
    {
        if (string.IsNullOrEmpty(GeneratedPassword))
            return;

        var dataPackage = new DataPackage
        {
            RequestedOperation = DataPackageOperation.Copy
        };
        dataPackage.SetText(GeneratedPassword);
        Clipboard.SetContent(dataPackage);

        WeakReferenceMessenger.Default.Send(
            new InAppNotificationMessage(
                ViewResourceLoader.GetString("PasswordCopiedToClipboard"),
                AppNotificationType.Success));
    }

    public bool CanSave() => !string.IsNullOrEmpty(GeneratedPassword);
    public Task OnSaveAsync()
    {
        if (!string.IsNullOrEmpty(GeneratedPassword))
        {
            WeakReferenceMessenger.Default.Send(
                new PasswordGeneratedForEditMessage(GeneratedPassword));
        }
        return Task.CompletedTask;
    }

    private void Clear()
    {
        GeneratedPassword = string.Empty;
        Length = PasswordConstants.SafeLength;
        LengthError = string.Empty;

        UseUppercase = true;
        UseLowercase = true;
        UseDigits = true;
        UseSpecial = false;
        ExcludeAmbiguous = false;

        Strength = PasswordStrength.Weak;
        StrengthBarWidth = 0;

        DialogContext.RaiseDialogContentChanged();
    }

    private static int CalculatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return 0;
        }

        int score = 0;

        if (password.Length >= 8) score += 2;
        if (password.Length >= 12) score += 2;
        if (password.Length >= 16) score += 2;

        if (ContainsUpper(password)) score += 1;
        if (ContainsLower(password)) score += 1;
        if (ContainsDigit(password)) score += 1;
        if (ContainsSpecial(password)) score += 1;

        return score;
    }
    private static PasswordStrength GetPasswordStrength(int score)
    {
        if (score <= PasswordConstants.WeakThreshold) return PasswordStrength.Weak;
        if (score <= PasswordConstants.MediumThreshold) return PasswordStrength.Medium;
        return PasswordStrength.Strong;
    }

    private void UpdateStrength()
    {
        var password = GeneratedPassword ?? string.Empty;

        var score = CalculatePasswordStrength(password);

        Strength = GetPasswordStrength(score);
    }
    private static bool ContainsUpper(string s) => s.Any(char.IsUpper);
    private static bool ContainsLower(string s) => s.Any(char.IsLower);
    private static bool ContainsDigit(string s) => s.Any(char.IsDigit);
    private static bool ContainsSpecial(string s) => s.Any(c => !char.IsLetterOrDigit(c));

    public Task OnCancelAsync()
    {
        return Task.CompletedTask;
    }
}

