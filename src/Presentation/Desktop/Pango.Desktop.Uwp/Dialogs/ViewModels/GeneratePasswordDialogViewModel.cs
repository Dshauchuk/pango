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
using Pango.Domain.Common;
using Serilog;
using Windows.ApplicationModel.DataTransfer;

namespace Pango.Desktop.Uwp.Dialogs.ViewModels;

/// <summary>
/// View model for the password generation dialog, handling settings, generation, and clipboard operations.
/// </summary>
public partial class GeneratePasswordDialogViewModel : ViewModelBase, IDialogViewModel
{
    #region Fields

    private readonly ISender _sender;
    private readonly IPasswordGeneratorSettingsService _settingsService;
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

    private readonly RamProtectedString _protectedGeneratedPassword = new(string.Empty);

    #endregion

    #region Properties

    /// <summary>
    /// Gets the generated password string.
    /// </summary>
    public string GeneratedPassword
    {
        get => _protectedGeneratedPassword.GetDecryptedValue();
        private set
        {
            if (_protectedGeneratedPassword.GetDecryptedValue() == value) return;

            _protectedGeneratedPassword.SetPlaintextValue(value);
            OnPropertyChanged(nameof(GeneratedPassword));
            UpdateStrength();
            CopyPasswordCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets or sets the desired password length.
    /// </summary>
    public int Length
    {
        get => _length;
        set
        {
            if (_length == value) return;
            _length = value;
            OnPropertyChanged(nameof(Length));
            ValidateLength(value);
            Log.Logger?.Debug("Password length set to: {Length}", value);
        }
    }

    /// <summary>
    /// Gets or sets the validation error message for password length.
    /// </summary>
    public string LengthError
    {
        get => _lengthError;
        set
        {
            if (_lengthError == value) return;
            _lengthError = value;
            OnPropertyChanged(nameof(LengthError));
        }
    }

    /// <summary>
    /// Gets or sets the validation error message for character set selection.
    /// </summary>
    public string CharsetsError
    {
        get => _charsetsError;
        set
        {
            if (_charsetsError == value) return;
            _charsetsError = value;
            OnPropertyChanged(nameof(CharsetsError));
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to include uppercase letters.
    /// </summary>
    public bool UseUppercase
    {
        get => _useUppercase;
        set
        {
            if (_useUppercase == value) return;
            _useUppercase = value;
            OnPropertyChanged(nameof(UseUppercase));
            Log.Logger?.Debug("UseUppercase set to: {Value}", value);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to include lowercase letters.
    /// </summary>
    public bool UseLowercase
    {
        get => _useLowercase;
        set
        {
            if (_useLowercase == value) return;
            _useLowercase = value;
            OnPropertyChanged(nameof(UseLowercase));
            Log.Logger?.Debug("UseLowercase set to: {Value}", value);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to include digits.
    /// </summary>
    public bool UseDigits
    {
        get => _useDigits;
        set
        {
            if (_useDigits == value) return;
            _useDigits = value;
            OnPropertyChanged(nameof(UseDigits));
            Log.Logger?.Debug("UseDigits set to: {Value}", value);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to include special characters.
    /// </summary>
    public bool UseSpecial
    {
        get => _useSpecial;
        set
        {
            if (_useSpecial == value) return;
            _useSpecial = value;
            OnPropertyChanged(nameof(UseSpecial));
            Log.Logger?.Debug("UseSpecial set to: {Value}", value);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to exclude ambiguous characters.
    /// </summary>
    public bool ExcludeAmbiguous
    {
        get => _excludeAmbiguous;
        set
        {
            if (_excludeAmbiguous == value) return;
            _excludeAmbiguous = value;
            OnPropertyChanged(nameof(ExcludeAmbiguous));
            Log.Logger?.Debug("ExcludeAmbiguous set to: {Value}", value);
        }
    }

    /// <summary>
    /// Gets the calculated password strength enumeration.
    /// </summary>
    public PasswordStrength Strength
    {
        get => _strength;
        private set
        {
            if (_strength == value) return;
            _strength = value;
            OnPropertyChanged(nameof(Strength));
            OnPropertyChanged(nameof(StrengthLabel));
            OnPropertyChanged(nameof(StrengthBrush));
        }
    }

    /// <summary>
    /// Gets the localized label for the current password strength.
    /// </summary>
    public string StrengthLabel =>
        Strength switch
        {
            PasswordStrength.Weak => ViewResourceLoader.GetString("PasswordStrength_Weak"),
            PasswordStrength.Medium => ViewResourceLoader.GetString("PasswordStrength_Medium"),
            PasswordStrength.Strong => ViewResourceLoader.GetString("PasswordStrength_Strong"),
            _ => string.Empty
        };

    /// <summary>
    /// Gets the brush color representing the current password strength.
    /// </summary>
    public SolidColorBrush StrengthBrush =>
        Strength switch
        {
            PasswordStrength.Weak => new SolidColorBrush(Colors.Red),
            PasswordStrength.Medium => new SolidColorBrush(Colors.Orange),
            PasswordStrength.Strong => new SolidColorBrush(Colors.Green),
            _ => new SolidColorBrush(Colors.Gray)
        };

    /// <summary>
    /// Gets or sets the width of the visual strength indicator bar.
    /// </summary>
    public double StrengthBarWidth
    {
        get => _strengthBarWidth;
        set => SetProperty(ref _strengthBarWidth, value);
    }

    /// <summary>
    /// Gets the dialog context for coordinating dialog lifecycle events.
    /// </summary>
    public IDialogContext DialogContext { get; }

    #endregion

    #region Commands

    /// <summary>
    /// Command for generating a new password with current settings.
    /// </summary>
    public IAsyncRelayCommand GenerateCommand { get; }

    /// <summary>
    /// Command for regenerating a password (alias for GenerateCommand).
    /// </summary>
    public IAsyncRelayCommand RegeneratePasswordCommand { get; }

    /// <summary>
    /// Command for copying the generated password to clipboard.
    /// </summary>
    public RelayCommand CopyPasswordCommand { get; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratePasswordDialogViewModel"/> class.
    /// </summary>
    /// <param name="sender">MediatR sender for dispatching commands.</param>
    /// <param name="logger">Logger instance for this view model.</param>
    /// <param name="settingsService">Service for persisting generator settings.</param>
    public GeneratePasswordDialogViewModel(
        ISender sender,
        ILogger<GeneratePasswordDialogViewModel> logger,
        IPasswordGeneratorSettingsService settingsService)
        : base(logger)
    {
        _sender = sender;
        _settingsService = settingsService;

        GenerateCommand = new AsyncRelayCommand(GenerateAsync);
        RegeneratePasswordCommand = new AsyncRelayCommand(GenerateAsync);
        CopyPasswordCommand = new RelayCommand(CopyPassword, CanCopyPassword);
        DialogContext = new DialogContext();

        WeakReferenceMessenger.Default.Register<UserSignedOutMessage>(this, (_, _) => Clear());

        Log.Logger?.Debug("GeneratePasswordDialogViewModel initialized");
    }

    #region Overrides

    /// <summary>
    /// Called when the dialog is navigated to: loads settings and initializes state.
    /// </summary>
    /// <param name="parameter">Optional dialog parameters containing pre-generated password.</param>
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        Log.Logger?.Debug("GeneratePasswordDialogViewModel navigated to");

        await base.OnNavigatedToAsync(parameter);

        var settings = _settingsService.Load();
        Length = settings.Length;
        UseUppercase = settings.UseUppercase;
        UseLowercase = settings.UseLowercase;
        UseDigits = settings.UseDigits;
        UseSpecial = settings.UseSpecial;
        ExcludeAmbiguous = settings.ExcludeAmbiguous;

        GeneratedPassword = string.Empty;
        Strength = PasswordStrength.Weak;
        StrengthBarWidth = 0;

        if (parameter is GeneratePasswordDialogParameters p && !string.IsNullOrEmpty(p.GeneratedPassword))
        {
            GeneratedPassword = p.GeneratedPassword;
            Log.Logger?.Debug("Pre-filled password from parameters: length {Length}", p.GeneratedPassword.Length);
        }

        DialogContext.RaiseDialogContentChanged();
        Log.Logger?.Debug("Dialog initialization completed");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Persists current generator settings to storage.
    /// </summary>
    private void SaveSettings()
    {
        Log.Logger?.Debug("Saving password generator settings");

        var settings = new PasswordGeneratorSettings
        {
            Length = Length,
            UseUppercase = UseUppercase,
            UseLowercase = UseLowercase,
            UseDigits = UseDigits,
            UseSpecial = UseSpecial,
            ExcludeAmbiguous = ExcludeAmbiguous
        };
        _settingsService.Save(settings);
    }

    /// <summary>
    /// Generates a new password asynchronously using configured options.
    /// </summary>
    private async Task GenerateAsync()
    {
        Log.Logger?.Information("GenerateAsync: generating password with length {Length}", Length);

        if (!ValidateLength(Length) || !ValidateCharsets())
        {
            Log.Logger?.Warning("GenerateAsync: validation failed");
            return;
        }

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
            Log.Logger?.Error("Password generation failed: {Errors}", string.Join(", ", result.Errors));
            var message = result.FirstError.Description ?? ViewResourceLoader.GetString("PasswordGenerationFailed");
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(message, AppNotificationType.Error));
            return;
        }

        GeneratedPassword = result.Value;
        SaveSettings();
        DialogContext.RaiseDialogContentChanged();

        WeakReferenceMessenger.Default.Send(
            new InAppNotificationMessage(ViewResourceLoader.GetString("PasswordGeneratedSuccessfully")));

        Log.Logger?.Information("Password generated successfully: length {Length}", result.Value.Length);
    }

    /// <summary>
    /// Validates that password length is within allowed range.
    /// </summary>
    /// <param name="value">The length value to validate.</param>
    /// <returns>True if valid; false otherwise.</returns>
    private bool ValidateLength(int value)
    {
        if (value < PasswordConstants.MinLength || value > PasswordConstants.MaxLength)
        {
            LengthError = ViewResourceLoader.GetString("PasswordLength_OutOfRange");
            Log.Logger?.Debug("ValidateLength failed: {Value} out of range [{Min}, {Max}]",
                value, PasswordConstants.MinLength, PasswordConstants.MaxLength);
            return false;
        }
        LengthError = string.Empty;
        return true;
    }

    /// <summary>
    /// Validates that at least one character set is selected.
    /// </summary>
    /// <returns>True if at least one charset is enabled; false otherwise.</returns>
    private bool ValidateCharsets()
    {
        if (!UseUppercase && !UseLowercase && !UseDigits && !UseSpecial)
        {
            CharsetsError = ViewResourceLoader.GetString("PasswordGeneration_Error_NoCharSet");
            Log.Logger?.Debug("ValidateCharsets failed: no character sets selected");
            return false;
        }
        CharsetsError = string.Empty;
        return true;
    }

    /// <summary>
    /// Determines if the copy command can execute based on password presence.
    /// </summary>
    /// <returns>True if generated password is not empty.</returns>
    private bool CanCopyPassword() => !string.IsNullOrEmpty(GeneratedPassword);

    /// <summary>
    /// Copies the generated password to system clipboard and shows confirmation.
    /// </summary>
    private void CopyPassword()
    {
        if (string.IsNullOrEmpty(GeneratedPassword))
        {
            Log.Logger?.Warning("CopyPassword called with empty password");
            return;
        }

        Log.Logger?.Debug("Copying password to clipboard: length {Length}", GeneratedPassword.Length);

        var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        dataPackage.SetText(GeneratedPassword);
        Clipboard.SetContent(dataPackage);

        WeakReferenceMessenger.Default.Send(
            new InAppNotificationMessage(
                ViewResourceLoader.GetString("PasswordCopiedToClipboard"),
                AppNotificationType.Success));
    }

    /// <summary>
    /// Determines if the dialog can be saved (password generated).
    /// </summary>
    /// <returns>True if generated password is not empty.</returns>
    public bool CanSave() => !string.IsNullOrEmpty(GeneratedPassword);

    /// <summary>
    /// Handles dialog save action: sends generated password via messenger.
    /// </summary>
    public Task OnSaveAsync()
    {
        Log.Logger?.Debug("OnSaveAsync called");

        if (!string.IsNullOrEmpty(GeneratedPassword))
        {
            WeakReferenceMessenger.Default.Send(
                new PasswordGeneratedForEditMessage(GeneratedPassword));
            Log.Logger?.Information("Password sent to edit view via messenger");
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles dialog cancel action: no-op implementation.
    /// </summary>
    public Task OnCancelAsync()
    {
        Log.Logger?.Debug("OnCancelAsync called");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets all properties to default values.
    /// </summary>
    private void Clear()
    {
        Log.Logger?.Debug("Clear: resetting all properties to defaults");

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

    /// <summary>
    /// Calculates a numeric strength score for the given password.
    /// </summary>
    /// <param name="password">The password string to evaluate.</param>
    /// <returns>Numeric score representing password strength.</returns>
    private static int CalculatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password)) return 0;

        int score = 0;
        if (password.Length >= 8) score += 2;
        if (password.Length >= 12) score += 2;
        if (password.Length >= 16) score += 2;

        bool hasUpper = false, hasLower = false, hasDigit = false, hasSpecial = false;

        foreach (char c in password)
        {
            if (!hasUpper && char.IsUpper(c)) { hasUpper = true; score++; }
            else if (!hasLower && char.IsLower(c)) { hasLower = true; score++; }
            else if (!hasDigit && char.IsDigit(c)) { hasDigit = true; score++; }
            else if (!hasSpecial && !char.IsLetterOrDigit(c)) { hasSpecial = true; score++; }
        }
        return score;
    }

    /// <summary>
    /// Maps a numeric strength score to PasswordStrength enumeration.
    /// </summary>
    /// <param name="score">The numeric strength score.</param>
    /// <returns>Corresponding PasswordStrength value.</returns>
    private static PasswordStrength GetPasswordStrength(int score)
    {
        if (score <= PasswordConstants.WeakThreshold) return PasswordStrength.Weak;
        if (score <= PasswordConstants.MediumThreshold) return PasswordStrength.Medium;
        return PasswordStrength.Strong;
    }

    /// <summary>
    /// Updates the visual strength indicator asynchronously on background thread.
    /// </summary>
    private void UpdateStrength()
    {
        var password = GeneratedPassword ?? string.Empty;
        Task.Run(() =>
        {
            var score = CalculatePasswordStrength(password);
            App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                Strength = GetPasswordStrength(score);
                Log.Logger?.Debug("Strength updated: {Strength} (score: {Score})", Strength, score);
            });
        });
    }

    #endregion
}
