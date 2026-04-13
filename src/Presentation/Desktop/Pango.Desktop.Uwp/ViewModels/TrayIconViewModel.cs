using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Views;
using System.Diagnostics;
using Windows.ApplicationModel.Resources;
using Windows.UI.Notifications;

namespace Pango.Desktop.Uwp.ViewModels
{
    /// <summary>
    /// ViewModel responsible for handling System Tray interactions and managing the Backup Service process.
    /// </summary>
    public partial class TrayIconViewModel : ObservableObject
    {
        private const string BackupProcessName = "Pango.BackupService";
        private readonly MainWindow _mainWindow;
        private readonly ILogger<TrayIconViewModel>? _logger;

        private bool _isBackupRunning = false;
        private bool _isAppVisible = true;
        private static bool _hasShownWindowsToast = false;
        private ResourceLoader _resourceLoader;

        /// <summary>
        /// Event triggered when the UI state (like backup running status or window visibility) changes.
        /// </summary>
        public event EventHandler? UIStateChanged;

        public string OpenPangoText { get; private set; } = string.Empty;
        public string ClosePangoText { get; private set; } = string.Empty;
        public string StartBackupText { get; private set; } = string.Empty;
        public string StopBackupText { get; private set; } = string.Empty;
        public string ExitText { get; private set; } = string.Empty;

        public TrayIconViewModel(MainWindow mainWindow, ILogger<TrayIconViewModel>? logger)
        {
            _mainWindow = mainWindow;
            _logger = logger;
            _resourceLoader = new ResourceLoader();

            UpdateTranslations();

            OpenAppCommand = new RelayCommand(OpenApp);
            CloseAppCommand = new RelayCommand(CloseApp);
            StartBackupCommand = new RelayCommand(StartBackup);
            StopBackupCommand = new RelayCommand(StopBackup);
            ExitCommand = new RelayCommand(Exit);

            WeakReferenceMessenger.Default.Register<Mvvm.Messages.BackupStateChangedMessage>(this, (r, m) =>
            {
                if (m.Value && !IsBackupRunning)
                    StartBackupInternal();
                else if (!m.Value && IsBackupRunning)
                    StopBackupInternal();
            });

            CheckExpirationsAndNotify();
        }

        public bool IsBackupRunning
        {
            get => _isBackupRunning;
            set
            {
                if (SetProperty(ref _isBackupRunning, value))
                    UIStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsAppVisible
        {
            get => _isAppVisible;
            set
            {
                if (SetProperty(ref _isAppVisible, value))
                    UIStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public IRelayCommand OpenAppCommand { get; }
        public IRelayCommand CloseAppCommand { get; }
        public IRelayCommand StartBackupCommand { get; }
        public IRelayCommand StopBackupCommand { get; }
        public IRelayCommand ExitCommand { get; }

        /// <summary>
        /// Updates the localized strings for the tray menu dynamically.
        /// </summary>
        public void UpdateTranslations()
        {
            try
            {
                _resourceLoader = new ResourceLoader();
                OpenPangoText = _resourceLoader.GetString("Tray_OpenPango");
                ClosePangoText = _resourceLoader.GetString("Tray_CloseWindow");
                StartBackupText = _resourceLoader.GetString("Tray_StartBackup");
                StopBackupText = _resourceLoader.GetString("Tray_StopBackup");
                ExitText = _resourceLoader.GetString("Tray_Exit");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to load tray menu translations.");
            }
        }

        /// <summary>
        /// Restores the main window from the system tray.
        /// </summary>
        private void OpenApp()
        {
            _mainWindow.ShowWindow();
            IsAppVisible = true;
        }

        /// <summary>
        /// Hides the main window to the system tray.
        /// </summary>
        private void CloseApp()
        {
            _mainWindow.HideWindow();
            IsAppVisible = false;
        }

        /// <summary>
        /// Starts the standalone backup service process executable.
        /// </summary>
        public void StartBackup()
        {
            WeakReferenceMessenger.Default.Send(new Mvvm.Messages.TrayBackupToggleMessage(true));
            StartBackupInternal();
        }

        /// <summary>
        /// Instantly kills the background backup process, freeing memory (like End Task in Task Manager).
        /// </summary>
        public void StopBackup()
        {
            WeakReferenceMessenger.Default.Send(new Mvvm.Messages.TrayBackupToggleMessage(false));
            StopBackupInternal();
        }

        /// <summary>
        /// Starts the standalone backup service process executable.
        /// </summary>
        private void StartBackupInternal()
        {
            try
            {
                var processes = Process.GetProcessesByName(BackupProcessName);
                if (processes.Length > 0)
                {
                    _logger?.LogInformation("Backup process is already running.");
                    IsBackupRunning = true;
                    return;
                }

                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string exePath = Path.Combine(appDir, BackupProcessName + ".exe");

                if (!File.Exists(exePath))
                {
                    _logger?.LogError("Backup Executable not found at path: {ExePath}.", exePath);
                    return;
                }

                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = appDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(psi);
                IsBackupRunning = true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start backup process.");
                IsBackupRunning = false;
            }
        }

        /// <summary>
        /// Instantly kills the background backup process.
        /// </summary>
        private void StopBackupInternal()
        {
            try
            {
                var processes = Process.GetProcessesByName(BackupProcessName);
                foreach (var process in processes)
                {
                    process.Kill();
                    process.WaitForExit();
                    process.Dispose();
                }
                IsBackupRunning = false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to kill backup process.");
            }
        }

        /// <summary>
        /// Checks the unencrypted cache for expiring passwords and shows a Windows Toast Notification
        /// </summary>
        private void CheckExpirationsAndNotify()
        {
            if (_hasShownWindowsToast) return;

            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);

                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                bool alertsEnabled = !localSettings.Values.TryGetValue(Constants.Settings.EnableExpirationAlerts, out var aVal) || (bool)aVal;
                bool windowsAlertsEnabled = !localSettings.Values.TryGetValue(Constants.Settings.EnableWindowsNotifications, out var wVal) || (bool)wVal;

                if (!alertsEnabled || !windowsAlertsEnabled) return;

                int warningDays = localSettings.Values.TryGetValue(Constants.Settings.ExpirationWarningDays, out var daysVal) ? (int)daysVal : 7;
                string cacheFile = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "expiration_cache.json");

                if (!File.Exists(cacheFile)) return;

                try
                {
                    var json = File.ReadAllText(cacheFile).Trim();

                    if (!json.StartsWith('{') || !json.EndsWith('}')) return;

                    Dictionary<string, List<ExpirationCacheItem>> dates;
                    try
                    {
                        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        dates = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<ExpirationCacheItem>>>(json, options) ?? [];
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        return;
                    }

                    if (dates.Count == 0) return;

                    var now = DateTime.Now.Date;
                    bool hasExpiring = false;

                    foreach (var userCache in dates.Values)
                    {
                        if (userCache.Any(i => (i.Date.LocalDateTime.Date - now).TotalDays <= warningDays))
                        {
                            hasExpiring = true;
                            break;
                        }
                    }

                    if (hasExpiring)
                    {
                        _hasShownWindowsToast = true;

                        string toastXmlString = $@"
                        <toast>
                            <visual>
                                <binding template='ToastGeneric'>
                                    <text>{_resourceLoader.GetString("Toast_Expiring_Title")}</text>
                                    <text>{_resourceLoader.GetString("Toast_Expiring_Body")}</text>
                                </binding>
                            </visual>
                        </toast>";

                        var xmlDoc = new Windows.Data.Xml.Dom.XmlDocument();
                        xmlDoc.LoadXml(toastXmlString);

                        var toast = new ToastNotification(xmlDoc);
                        ToastNotificationManager.CreateToastNotifier().Show(toast);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to check expirations and show Toast.");
                }
            });
        }

        /// <summary>
        /// Completely terminates the application and stops the background backup.
        /// </summary>
        private void Exit()
        {
            StopBackup();
            _mainWindow.ForceExit();
        }
    }
}