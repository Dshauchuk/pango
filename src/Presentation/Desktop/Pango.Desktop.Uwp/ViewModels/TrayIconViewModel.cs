using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Views;
using System;
using System.Diagnostics;
using System.IO;
using Windows.ApplicationModel.Resources;

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
                    _logger?.LogError("Backup Executable not found at path: {ExePath}. Ensure the BackupService is referenced and copied to the output directory.", exePath);
                    return;
                }

                if (_logger?.IsEnabled(LogLevel.Information) ?? false)
                {
                    _logger.LogInformation("Attempting to start backup service from: {ExePath}", exePath);
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
                _logger?.LogInformation("Backup service started successfully.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start backup process.");
                IsBackupRunning = false;
            }
        }

        /// <summary>
        /// Instantly kills the background backup process, freeing memory (like End Task in Task Manager).
        /// </summary>
        public void StopBackup()
        {
            try
            {
                var processes = Process.GetProcessesByName(BackupProcessName);
                if (processes.Length == 0)
                {
                    _logger?.LogInformation("No running backup processes found to kill.");
                }

                foreach (var process in processes)
                {
                    if (_logger?.IsEnabled(LogLevel.Information) ?? false)
                    {
                        _logger.LogInformation("Killing backup process with ID: {Id}", process.Id);
                    }
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
        /// Completely terminates the application and stops the background backup.
        /// </summary>
        private void Exit()
        {
            StopBackup();
            _mainWindow.ForceExit();
        }
    }
}