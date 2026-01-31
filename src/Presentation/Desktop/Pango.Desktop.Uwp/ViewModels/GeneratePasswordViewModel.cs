using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualBasic;
using Pango.Application.UseCases.Password.Commands.GeneratePassword;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Mvvm.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;

namespace Pango.Desktop.Uwp.ViewModels
{
    public sealed class GeneratePasswordViewModel : ViewModelBase
    {
        public string GeneratedPassword { get; set; } = string.Empty;
        public int Length { get; set; } = 16;

        public bool UseUppercase { get; set; } = true;
        public bool UseLowercase { get; set; } = true;
        public bool UseDigits { get; set; } = true;
        public bool UseSpecial { get; set; } = false;
        public bool ExcludeAmbiguous { get; set; } = false;

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
        #endregion Properties

        #region Commands
        public ICommand GenerateCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand CancelCommand { get; }
        public RelayCommand CopyPasswordCommand { get; }
        public ICommand RegeneratePasswordCommand { get; }
        #endregion Commands

        private async Task GenerateAsync()
        {
            var command = new GeneratePasswordCommand(Length, UseUppercase, UseLowercase, UseDigits, UseSpecial, ExcludeAmbiguous);
            var result = await _sender.Send(command);
            if (result.IsError)
            {
                Logger.LogError("Password generation failed: {Errors}", string.Join(", ", result.Errors));

                var message = result.FirstError.Description ?? ViewResourceLoader.GetString("PasswordGenerationFailed");

                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(message, AppNotificationType.Error));

                return;
            }
            GeneratedPassword = result.Value;

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(ViewResourceLoader.GetString("PasswordGeneratedSuccessfully")));
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

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(ViewResourceLoader.GetString("PasswordCopiedToClipboard")));
        }

        public GeneratePasswordViewModel(ILogger<GeneratePasswordViewModel> logger) : base(logger)
        {
            GenerateCommand = new RelayCommand(() => {  });
            ApplyCommand = new RelayCommand(() => {  });
            CancelCommand = new RelayCommand(() => {  });
            CopyPasswordCommand = new RelayCommand(() => {  });
            RegeneratePasswordCommand = new RelayCommand(() => {  });
        }
        private static bool ContainsUpper(string s) => s.Any(char.IsUpper);
        private static bool ContainsLower(string s) => s.Any(char.IsLower);
        private static bool ContainsDigit(string s) => s.Any(char.IsDigit);
        private static bool ContainsSpecial(string s) => s.Any(c => !char.IsLetterOrDigit(c));
    }

}
