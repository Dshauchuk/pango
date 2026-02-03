using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Pango.Desktop.Uwp.Core.Enums;
using System.Windows.Input;

namespace Pango.Desktop.Uwp.ViewModels
{
    public sealed class GeneratePasswordViewModel : ViewModelBase
    {
        public string GeneratedPassword { get; set; } = string.Empty;

        private int _length = 16;
        public int Length
        {
            get => _length;
            private set
            {
                if (_length == value)
                    return;

                _length = value;
                OnPropertyChanged(nameof(Length));
            }
        }

        private string _lengthText = "16";
        public string LengthText
        {
            get => _lengthText;
            set
            {
                if (_lengthText == value)
                    return;

                _lengthText = value;
                OnPropertyChanged(nameof(LengthText));

                ValidateLengthCore(value);
            }
        }

        private string _lengthError;
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

        private void ValidateLengthCore(string text)
        {
            if (!int.TryParse(text, out var parsed))
            {
                LengthError = ViewResourceLoader.GetString("PasswordLength_Invalid");
                return;
            }

            if (parsed < 8 || parsed > 64)
            {
                LengthError = ViewResourceLoader.GetString("PasswordLength_OutOfRange");
                return;
            }

            LengthError = string.Empty;
            Length = parsed;
        }
        public bool UseUppercase { get; set; } = true;
        public bool UseLowercase { get; set; } = true;
        public bool UseDigits { get; set; } = true;
        public bool UseSpecial { get; set; } = false;
        public bool ExcludeAmbiguous { get; set; } = false;

        private PasswordStrength _strength;
        public PasswordStrength Strength
        {
            get => _strength;
            set
            {
                if (_strength != value)
                {
                    _strength = value;
                    OnPropertyChanged(nameof(Strength));
                    OnPropertyChanged(nameof(StrengthLabel));
                    OnPropertyChanged(nameof(StrengthBrush));
                }
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

        public ICommand GenerateCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CopyPasswordCommand { get; }
        public ICommand RegeneratePasswordCommand { get; }

        public GeneratePasswordViewModel(ILogger<GeneratePasswordViewModel> logger) : base(logger)
        {
            GenerateCommand = new RelayCommand(() => { });
            ApplyCommand = new RelayCommand(() => { });
            CancelCommand = new RelayCommand(() => { });
            CopyPasswordCommand = new RelayCommand(() => { });
            RegeneratePasswordCommand = new RelayCommand(() => { });
        }
    }

}
