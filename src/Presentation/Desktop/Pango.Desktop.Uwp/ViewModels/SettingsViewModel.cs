using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Desktop.Uwp.Core;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Core.Utility.Contracts;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.Views;
using Pango.Persistence;
using Serilog;
using System.Collections.ObjectModel;
using System.Text.Json;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;

namespace Pango.Desktop.Uwp.ViewModels;

/// <summary>
/// View model for application settings management including appearance, security, data storage, and backup configuration.
/// </summary>
[AppView(AppView.Settings)]
public partial class SettingsViewModel : ViewModelBase
{
    #region Nested Types

    /// <summary>
    /// Represents available import destination options for user data.
    /// </summary>
    public enum ImportDestination { Root = 0, Folder = 1 }

    /// <summary>
    /// Data class for import destination dropdown options with display content and enum value.
    /// </summary>
    public class ImportDestinationOption
    {
        /// <summary>
        /// Gets or sets the display text for the option.
        /// </summary>
        public required string Content { get; set; }

        /// <summary>
        /// Gets or sets the enum value representing the destination type.
        /// </summary>
        public ImportDestination Value { get; set; }
    }

    #endregion

    #region Fields

    // Services
    private readonly IUserContextProvider _userContextProvider;
    private readonly IUserStorageManager _userStorageManager;
    private readonly IAppDomainProvider _appDomainProvider;
    private readonly IAppMetaService _appMetaService;
    private readonly IStartupService _startupService;
    private readonly ResourceLoader _resourceLoader;

    // Appearance & Behavior
    private AppLanguage _selectedLanguage;
    private AppTheme _selectedAppTheme;
    private bool _allowAutolock;
    private bool _allowLaunchAtStartup;
    private KeyValuePair<int, string>? _selectedLockOnIdleInMinutesItem;
    private ImportDestination _selectedImportDestination;
    private ObservableCollection<ImportDestinationOption> _importDestinationOptions = [];
    private string _selectedDataFolderPath;
    private bool _isExpirationAlertsEnabled;
    private int _expirationWarningDays;
    private bool _isChangingDataFolder;
    private string _dataFolderProgressText = string.Empty;
    private bool _showExpirationWindowOnStartup;
    private bool _isWindowsNotificationsEnabled;
    private bool _isInitializing;

    // Backup Configuration
    private string _configPath = string.Empty;
    private string _backupPath = string.Empty;
    private int _selectedBackupInterval;
    private int _retentionDays;
    private bool _isBackupServiceEnabled;
    private string _backupPassword = string.Empty;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class with required services.
    /// </summary>
    /// <param name="logger">Logger instance for this view model.</param>
    /// <param name="userContextProvider">Service for accessing current user context.</param>
    /// <param name="userStorageManager">Service for managing user data storage operations.</param>
    /// <param name="appDomainProvider">Service for application domain and path management.</param>
    /// <param name="appMetaService">Service for application metadata retrieval.</param>
    /// <param name="startupService">Service for managing application startup registration.</param>
    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        IUserContextProvider userContextProvider,
        IUserStorageManager userStorageManager,
        IAppDomainProvider appDomainProvider,
        IAppMetaService appMetaService,
        IStartupService startupService)
        : base(logger)
    {
        _userContextProvider = userContextProvider;
        _userStorageManager = userStorageManager;
        _appDomainProvider = appDomainProvider;
        _appMetaService = appMetaService;
        _startupService = startupService;
        _resourceLoader = new ResourceLoader();

        Languages = [.. AppLanguage.GetAppLanguageCollection()];
        AppThemes = [];
        LockOnIdleInMinutesItems = [];
        BackupIntervals = [5, 10, 15, 20, 30, 60, 120, 180];

        _selectedDataFolderPath = appDomainProvider.GetAppDataFolderPath();

        ImportDestinationOptions =
        [
            new() { Content = _resourceLoader.GetString("ImportDestination_Root"), Value = ImportDestination.Root },
            new() { Content = _resourceLoader.GetString("ImportDestination_Folder"), Value = ImportDestination.Folder }
        ];

        _selectedImportDestination = LoadImportSetting();

        _isExpirationAlertsEnabled = (bool?)ApplicationData.Current.LocalSettings.Values[Constants.Settings.EnableExpirationAlerts] ?? true;
        _expirationWarningDays = (int?)ApplicationData.Current.LocalSettings.Values[Constants.Settings.ExpirationWarningDays] ?? 7;
        _showExpirationWindowOnStartup = (bool?)ApplicationData.Current.LocalSettings.Values[Constants.Settings.ShowExpirationWindowOnStartup] ?? true;
        _isWindowsNotificationsEnabled = (bool?)ApplicationData.Current.LocalSettings.Values[Constants.Settings.EnableWindowsNotifications] ?? true;

        InitializeDisplayResources();

        string appliedLocale = AppLanguageHelper.GetAppliedAppLanguage().Locale;
        _selectedLanguage = Languages.FirstOrDefault(e => e.Locale == appliedLocale) ?? Languages.First();

        var currentThemeVal = (int)AppThemeHelper.Theme;
        _selectedAppTheme = AppThemes.FirstOrDefault(e => e.Value == currentThemeVal) ?? AppThemes.First();

        int? blockAppAfterIdleMinutes = (int?)ApplicationData.Current.LocalSettings.Values[Constants.Settings.BlockAppAfterIdleMinutes];
        if (blockAppAfterIdleMinutes.HasValue)
        {
            _selectedLockOnIdleInMinutesItem = LockOnIdleInMinutesItems.FirstOrDefault(e => e.Key == blockAppAfterIdleMinutes);
            _allowAutolock = true;
        }

        Log.Logger?.Debug("SettingsViewModel initialized for user: {UserName}", _userContextProvider.GetUserName());
    }

    #endregion

    #region Properties - General

    /// <summary>
    /// Gets the current application version string.
    /// </summary>
    public string Version => _appMetaService.GetAppVersion();

    #endregion

    #region Properties - Appearance & Language

    /// <summary>
    /// Gets the collection of available application languages.
    /// </summary>
    public ObservableCollection<AppLanguage> Languages { get; private set; }

    /// <summary>
    /// Gets or sets the currently selected application language.
    /// </summary>
    public AppLanguage SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (value is not null && SetProperty(ref _selectedLanguage, value))
            {
                Log.Logger?.Information("Language changed to: {Locale}", value.Locale);
                AppLanguageHelper.ChangeAppLanguage(value, typeof(SettingsView));
                InitializeDisplayResources();
            }
        }
    }

    /// <summary>
    /// Gets the collection of available application themes.
    /// </summary>
    public ObservableCollection<AppTheme> AppThemes { get; private set; }

    /// <summary>
    /// Gets or sets the currently selected application theme.
    /// </summary>
    public AppTheme SelectedAppTheme
    {
        get => _selectedAppTheme;
        set
        {
            if (value != null && SetProperty(ref _selectedAppTheme, value))
            {
                Log.Logger?.Information("Theme changed to: {Theme}", (ElementTheme)value.Value);
                AppThemeHelper.SetTheme((ElementTheme)value.Value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the number of days before expiration to show warnings.
    /// </summary>
    public int ExpirationWarningDays
    {
        get => _expirationWarningDays;
        set
        {
            if (SetProperty(ref _expirationWarningDays, value))
            {
                Log.Logger?.Debug("Expiration warning days set to: {Days}", value);
                ApplicationData.Current.LocalSettings.Values[Constants.Settings.ExpirationWarningDays] = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to show expiration window on application startup.
    /// </summary>
    public bool ShowExpirationWindowOnStartup
    {
        get => _showExpirationWindowOnStartup;
        set
        {
            if (SetProperty(ref _showExpirationWindowOnStartup, value))
            {
                Log.Logger?.Debug("Show expiration window on startup: {Value}", value);
                ApplicationData.Current.LocalSettings.Values[Constants.Settings.ShowExpirationWindowOnStartup] = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether Windows native notifications are enabled.
    /// </summary>
    public bool IsWindowsNotificationsEnabled
    {
        get => _isWindowsNotificationsEnabled;
        set
        {
            if (SetProperty(ref _isWindowsNotificationsEnabled, value))
            {
                Log.Logger?.Debug("Windows notifications enabled: {Value}", value);
                ApplicationData.Current.LocalSettings.Values[Constants.Settings.EnableWindowsNotifications] = value;
            }
        }
    }

    #endregion

    #region Properties - Data Storage

    /// <summary>
    /// Gets or sets the currently selected data folder path.
    /// </summary>
    public string SelectedDataFolderPath
    {
        get => _selectedDataFolderPath;
        set => SetProperty(ref _selectedDataFolderPath, value);
    }

    /// <summary>
    /// Gets or sets whether expiration alerts are enabled.
    /// </summary>
    public bool IsExpirationAlertsEnabled
    {
        get => _isExpirationAlertsEnabled;
        set
        {
            if (SetProperty(ref _isExpirationAlertsEnabled, value))
            {
                Log.Logger?.Debug("Expiration alerts enabled: {Value}", value);
                ApplicationData.Current.LocalSettings.Values[Constants.Settings.EnableExpirationAlerts] = value;
            }
        }
    }

    /// <summary>
    /// Gets the collection of available expiration warning day intervals.
    /// </summary>
    public ObservableCollection<int> ExpirationWarningDaysItems { get; } = [1, 7, 14, 28];

    /// <summary>
    /// Gets or sets whether a data folder change operation is in progress.
    /// </summary>
    public bool IsChangingDataFolder
    {
        get => _isChangingDataFolder;
        set => SetProperty(ref _isChangingDataFolder, value);
    }

    /// <summary>
    /// Gets or sets the progress text displayed during data folder operations.
    /// </summary>
    public string DataFolderProgressText
    {
        get => _dataFolderProgressText;
        set => SetProperty(ref _dataFolderProgressText, value);
    }

    #endregion

    #region Properties - Security (Autolock)

    /// <summary>
    /// Gets the collection of available auto-lock idle time intervals.
    /// </summary>
    public ObservableCollection<KeyValuePair<int, string>> LockOnIdleInMinutesItems { get; private set; }

    /// <summary>
    /// Gets or sets the selected auto-lock idle time interval.
    /// </summary>
    public KeyValuePair<int, string>? SelectedLockOnIdleInMinutesItem
    {
        get => _selectedLockOnIdleInMinutesItem;
        set
        {
            Log.Logger?.Debug("Auto-lock idle time set to: {Minutes} minutes", value?.Key ?? 0);
            ApplicationData.Current.LocalSettings.Values[Constants.Settings.BlockAppAfterIdleMinutes] = value?.Key;
            WeakReferenceMessenger.Default.Send(new AutolockIdleChangedMessage(value?.Key));
            SetProperty(ref _selectedLockOnIdleInMinutesItem, value);
        }
    }

    /// <summary>
    /// Gets or sets whether automatic locking after idle is enabled.
    /// </summary>
    public bool AllowAutolock
    {
        get => _allowAutolock;
        set
        {
            if (SetProperty(ref _allowAutolock, value))
            {
                Log.Logger?.Information("Auto-lock feature: {Enabled}", value ? "enabled" : "disabled");

                if (value && SelectedLockOnIdleInMinutesItem == null && LockOnIdleInMinutesItems.Count > 2)
                    SelectedLockOnIdleInMinutesItem = LockOnIdleInMinutesItems[2];

                if (!value)
                    SelectedLockOnIdleInMinutesItem = null;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the application should launch at Windows startup.
    /// </summary>
    public bool AllowLaunchAtStartup
    {
        get => _allowLaunchAtStartup;
        set
        {
            if (SetProperty(ref _allowLaunchAtStartup, value))
            {
                Log.Logger?.Information("Launch at startup: {Enabled}", value ? "enabled" : "disabled");

                if (!_isInitializing)
                {
                    _ = ToggleStartupAsync(value);
                }
            }
        }
    }

    #endregion

    #region Properties - Backup

    /// <summary>
    /// Gets the collection of available backup interval options in minutes.
    /// </summary>
    public ObservableCollection<int> BackupIntervals { get; }

    /// <summary>
    /// Gets or sets the configured backup destination folder path.
    /// </summary>
    public string BackupPath
    {
        get => _backupPath;
        set => SetProperty(ref _backupPath, value);
    }

    /// <summary>
    /// Gets or sets the selected backup interval in minutes.
    /// </summary>
    public int SelectedBackupInterval
    {
        get => _selectedBackupInterval;
        set => SetProperty(ref _selectedBackupInterval, value);
    }

    /// <summary>
    /// Gets or sets the number of days to retain backup files.
    /// </summary>
    public int RetentionDays
    {
        get => _retentionDays;
        set
        {
            if (value < 1) value = 1;
            if (value > 365) value = 365;
            if (SetProperty(ref _retentionDays, value))
            {
                Log.Logger?.Debug("Backup retention days set to: {Days}", value);
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the backup service is currently enabled.
    /// </summary>
    public bool IsBackupServiceEnabled
    {
        get => _isBackupServiceEnabled;
        set
        {
            if (SetProperty(ref _isBackupServiceEnabled, value))
            {
                Log.Logger?.Information("Backup service: {Enabled}", value ? "enabled" : "disabled");
                WeakReferenceMessenger.Default.Send(new BackupStateChangedMessage(value));
                _ = SaveBackupSettingsAsync(showNotification: false);
            }
        }
    }

    /// <summary>
    /// Gets or sets the password used for encrypting backup files.
    /// </summary>
    public string BackupPassword
    {
        get => _backupPassword;
        set => SetProperty(ref _backupPassword, value);
    }

    /// <summary>
    /// Gets the collection of import destination options for the UI.
    /// </summary>
    public ObservableCollection<ImportDestinationOption> ImportDestinationOptions
    {
        get => _importDestinationOptions;
        private set => SetProperty(ref _importDestinationOptions, value);
    }

    /// <summary>
    /// Gets or sets the selected import destination option.
    /// </summary>
    public ImportDestinationOption? SelectedImportDestination
    {
        get => ImportDestinationOptions.FirstOrDefault(x => x.Value == _selectedImportDestination);
        set
        {
            if (value != null && SetProperty(ref _selectedImportDestination, value.Value))
            {
                Log.Logger?.Debug("Import destination set to: {Destination}", value.Value);
                SaveImportSetting(value.Value);
            }
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command for selecting a new data folder location.
    /// </summary>
    private RelayCommand? _selectDataFolderCommand;

    /// <summary>
    /// Gets the command for selecting a new data folder location.
    /// </summary>
    public RelayCommand SelectDataFolderCommand => _selectDataFolderCommand ??= new RelayCommand(async () => await SelectDataFolderAsync());

    /// <summary>
    /// Command for resetting data folder to default location.
    /// </summary>
    private IAsyncRelayCommand? _resetDataFolderCommand;

    /// <summary>
    /// Gets the command for resetting data folder to default location.
    /// </summary>
    public IAsyncRelayCommand ResetDataFolderCommand => _resetDataFolderCommand ??= new AsyncRelayCommand(ResetDataFolderAsync);

    #endregion

    #region Commands - Backup

    /// <summary>
    /// Command for browsing and selecting a backup folder location.
    /// </summary>
    private RelayCommand? _browseBackupPathCommand;

    /// <summary>
    /// Gets the command for browsing and selecting a backup folder location.
    /// </summary>
    public RelayCommand BrowseBackupPathCommand => _browseBackupPathCommand ??= new RelayCommand(async () => await BrowseBackupPathAsync());

    /// <summary>
    /// Command for resetting backup path to default location.
    /// </summary>
    private RelayCommand? _setDefaultBackupPathCommand;

    /// <summary>
    /// Gets the command for resetting backup path to default location.
    /// </summary>
    public RelayCommand SetDefaultBackupPathCommand => _setDefaultBackupPathCommand ??= new RelayCommand(SetDefaultBackupPath);

    /// <summary>
    /// Command for opening the configured backup folder in File Explorer.
    /// </summary>
    private RelayCommand? _openBackupFolderCommand;

    /// <summary>
    /// Gets the command for opening the configured backup folder in File Explorer.
    /// </summary>
    public RelayCommand OpenBackupFolderCommand => _openBackupFolderCommand ??= new RelayCommand(async () => await OpenBackupFolderAsync());

    /// <summary>
    /// Command for saving current backup configuration settings.
    /// </summary>
    private RelayCommand? _saveBackupSettingsCommand;

    /// <summary>
    /// Gets the command for saving current backup configuration settings.
    /// </summary>
    public RelayCommand SaveBackupSettingsCommand => _saveBackupSettingsCommand ??= new RelayCommand(async () => await SaveBackupSettingsAsync());

    /// <summary>
    /// Command for discarding changes and reloading backup settings.
    /// </summary>
    private RelayCommand? _cancelBackupChangesCommand;

    /// <summary>
    /// Gets the command for discarding changes and reloading backup settings.
    /// </summary>
    public RelayCommand CancelBackupChangesCommand => _cancelBackupChangesCommand ??= new RelayCommand(async () => await LoadBackupSettingsAsync());

    #endregion

    #region Overrides

    /// <summary>
    /// Called when the view is navigated to: initializes startup and backup configuration asynchronously.
    /// </summary>
    /// <param name="parameter">Optional navigation parameter.</param>
    public override async Task OnNavigatedToAsync(object? parameter)
    {
        Log.Logger?.Debug("SettingsViewModel navigated to");

        await base.OnNavigatedToAsync(parameter);
        _isInitializing = true;

        _ = Task.Run(async () =>
        {
            await CheckStartupStatusAsync();
            await InitializeBackupConfigAsync();
            _isInitializing = false;
            Log.Logger?.Debug("SettingsViewModel background initialization completed");
        });
    }

    /// <summary>
    /// Registers message subscriptions for tray backup toggle events.
    /// </summary>
    protected override void RegisterMessages()
    {
        base.RegisterMessages();

        WeakReferenceMessenger.Default.Register<TrayBackupToggleMessage>(this, (_, m) =>
        {
            App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(async () =>
            {
                Log.Logger?.Debug("TrayBackupToggleMessage received: {Value}", m.Value);
                IsBackupServiceEnabled = m.Value;
                await SaveBackupSettingsAsync(false);
            });
        });
    }

    #endregion

    #region Methods - Startup Implementation

    /// <summary>
    /// Checks and updates the UI with current startup registration status.
    /// </summary>
    private async Task CheckStartupStatusAsync()
    {
        bool enabled = await _startupService.IsStartupEnabledAsync();
        Log.Logger?.Debug("Startup registration status: {Enabled}", enabled);

        App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(() =>
        {
            SetProperty(ref _allowLaunchAtStartup, enabled, nameof(AllowLaunchAtStartup));
        });
    }

    /// <summary>
    /// Enables or disables application launch at Windows startup.
    /// </summary>
    /// <param name="enable">True to enable startup launch; false to disable.</param>
    private async Task ToggleStartupAsync(bool enable)
    {
        Log.Logger?.Information("Toggle startup registration: {Enable}", enable);

        if (enable)
        {
            bool success = await _startupService.EnableStartupAsync();
            if (!success)
            {
                Log.Logger?.Warning("Failed to enable startup registration");
                SetProperty(ref _allowLaunchAtStartup, false, nameof(AllowLaunchAtStartup));
            }
        }
        else
        {
            await _startupService.DisableStartupAsync();
            Log.Logger?.Information("Startup registration disabled");
        }
    }

    #endregion

    #region Methods - Select data folder

    /// <summary>
    /// Opens folder picker and migrates application data to the selected location asynchronously.
    /// </summary>
    private async Task SelectDataFolderAsync()
    {
        Log.Logger?.Information("SelectDataFolderAsync: opening folder picker");

        var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.Desktop };
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.CurrentWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder == null)
        {
            Log.Logger?.Debug("Folder picker cancelled by user");
            return;
        }

        bool confirmed = await ConfirmAsync(
            ViewResourceLoader.GetString("ChangeDataFolder"),
            ViewResourceLoader.GetString("ChangeDataFolder_Confirmation"));

        if (!confirmed)
        {
            Log.Logger?.Debug("Data folder change cancelled by user confirmation");
            return;
        }

        string oldPath = _appDomainProvider.GetAppDataFolderPath();
        string newPath = folder.Path;

        if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
        {
            Log.Logger?.Debug("Selected folder is same as current data folder");
            return;
        }

        try
        {
            IsChangingDataFolder = true;
            DataFolderProgressText = ViewResourceLoader.GetString("CopyingData");
            Log.Logger?.Information("Migrating data from {OldPath} to {NewPath}", oldPath, newPath);

            await Task.Run(() => _userStorageManager.MigrateDataAsync(oldPath, newPath));

            _appDomainProvider.ResetCache();
            DataFolderProgressText = ViewResourceLoader.GetString("UpdatingConfiguration");

            StorageApplicationPermissions.FutureAccessList.AddOrReplace(Constants.Settings.CustomDataFolderToken, folder);
            await _appDomainProvider.TryGetCustomDataFolderPathAsync();

            SelectedDataFolderPath = folder.Path;

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                ViewResourceLoader.GetString("DataFolderChanged"), AppNotificationType.Success));

            Log.Logger?.Information("Data folder successfully changed to: {NewPath}", newPath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to change data folder: {Message}", ex.Message);
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                ViewResourceLoader.GetString("DataFolderChangeFailed"), AppNotificationType.Warning));
        }
        finally
        {
            IsChangingDataFolder = false;
            DataFolderProgressText = string.Empty;
            Log.Logger?.Debug("Data folder change operation completed");
        }
    }

    /// <summary>
    /// Resets data folder to default location and migrates existing data asynchronously.
    /// </summary>
    private async Task ResetDataFolderAsync()
    {
        Log.Logger?.Information("ResetDataFolderAsync: resetting to default location");

        bool confirmed = await ConfirmAsync(
            ViewResourceLoader.GetString("ChangeDataFolder"),
            ViewResourceLoader.GetString("ChangeDataFolder_Confirmation"));

        if (!confirmed)
        {
            Log.Logger?.Debug("Data folder reset cancelled by user confirmation");
            return;
        }

        string oldPath = _appDomainProvider.GetAppDataFolderPath();

        if (StorageApplicationPermissions.FutureAccessList.ContainsItem(Constants.Settings.CustomDataFolderToken))
            StorageApplicationPermissions.FutureAccessList.Remove(Constants.Settings.CustomDataFolderToken);

        _appDomainProvider.ResetCache();
        string newPath = _appDomainProvider.GetAppDataFolderPath();

        if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
        {
            Log.Logger?.Debug("Already using default data folder");
            return;
        }

        try
        {
            IsChangingDataFolder = true;
            DataFolderProgressText = ViewResourceLoader.GetString("CopyingData");
            Log.Logger?.Information("Migrating data from {OldPath} to default {NewPath}", oldPath, newPath);

            await Task.Run(() => _userStorageManager.MigrateDataAsync(oldPath, newPath));

            SelectedDataFolderPath = newPath;

            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                ViewResourceLoader.GetString("DataFolderChanged"), AppNotificationType.Success));

            Log.Logger?.Information("Data folder successfully reset to default: {NewPath}", newPath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to reset data folder: {Message}", ex.Message);
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                ViewResourceLoader.GetString("DataFolderChangeFailed"), AppNotificationType.Warning));
        }
        finally
        {
            IsChangingDataFolder = false;
            DataFolderProgressText = string.Empty;
            Log.Logger?.Debug("Data folder reset operation completed");
        }
    }

    #endregion

    #region Methods - Backup Implementation

    /// <summary>
    /// Refreshes localized display resources for themes, autolock options, and import destinations.
    /// </summary>
    private void InitializeDisplayResources()
    {
        Log.Logger?.Debug("InitializeDisplayResources: refreshing localized UI resources");

        // 1. Refresh Themes
        var currentThemeVal = _selectedAppTheme?.Value ?? (int)AppThemeHelper.Theme;

        AppThemes.Clear();
        AppThemes.Add(new AppTheme { Value = (int)ElementTheme.Default, Name = _resourceLoader.GetString("Theme_Default") });
        AppThemes.Add(new AppTheme { Value = (int)ElementTheme.Light, Name = _resourceLoader.GetString("Theme_Light") });
        AppThemes.Add(new AppTheme { Value = (int)ElementTheme.Dark, Name = _resourceLoader.GetString("Theme_Dark") });

        _selectedAppTheme = AppThemes.FirstOrDefault(t => t.Value == currentThemeVal) ?? AppThemes.First();
        OnPropertyChanged(nameof(SelectedAppTheme));

        // 2. Refresh Autolock Options
        string localizedMinutes = _resourceLoader.GetString("Minute(-s)");
        var currentIdleKey = _selectedLockOnIdleInMinutesItem?.Key;

        LockOnIdleInMinutesItems.Clear();
        LockOnIdleInMinutesItems.Add(new(1, $"1 {localizedMinutes}"));
        LockOnIdleInMinutesItems.Add(new(3, $"3 {localizedMinutes}"));
        LockOnIdleInMinutesItems.Add(new(5, $"5 {localizedMinutes}"));
        LockOnIdleInMinutesItems.Add(new(10, $"10 {localizedMinutes}"));
        LockOnIdleInMinutesItems.Add(new(15, $"15 {localizedMinutes}"));
        LockOnIdleInMinutesItems.Add(new(30, $"30 {localizedMinutes}"));

        if (currentIdleKey.HasValue)
            _selectedLockOnIdleInMinutesItem = LockOnIdleInMinutesItems.FirstOrDefault(k => k.Key == currentIdleKey);

        OnPropertyChanged(nameof(SelectedLockOnIdleInMinutesItem));

        // Restore selected item based on saved Value
        OnPropertyChanged(nameof(SelectedImportDestination));

        Log.Logger?.Debug("Display resources refreshed successfully");
    }

    /// <summary>
    /// Initializes backup configuration by locating or creating the config file path.
    /// </summary>
    private async Task InitializeBackupConfigAsync()
    {
        Log.Logger?.Debug("InitializeBackupConfigAsync: setting up config path");

        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string configDir = Path.Combine(commonAppData, "Pango");

        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
            Log.Logger?.Debug("Created backup config directory: {ConfigDir}", configDir);
        }

        _configPath = Path.Combine(configDir, "backup_config.json");
        await LoadBackupSettingsAsync();
    }

    /// <summary>
    /// Loads backup settings from configuration file and updates UI properties.
    /// </summary>
    private async Task LoadBackupSettingsAsync()
    {
        Log.Logger?.Debug("LoadBackupSettingsAsync: loading from {ConfigPath}", _configPath);

        try
        {
            BackupSettings? settings = null;
            if (File.Exists(_configPath))
            {
                string json = await File.ReadAllTextAsync(_configPath);
                settings = JsonSerializer.Deserialize<BackupSettings>(json);
                Log.Logger?.Debug("Backup settings loaded from file");
            }

            settings ??= new BackupSettings();
            settings.Users ??= [];

            App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                BackupPath = string.IsNullOrEmpty(settings.TargetFolderPath) ? GetDefaultPath() : settings.TargetFolderPath;
                SelectedBackupInterval = settings.IntervalMinutes > 0 ? settings.IntervalMinutes : 60;
                IsBackupServiceEnabled = settings.IsEnabled;
                RetentionDays = settings.RetentionDays > 0 ? settings.RetentionDays : 7;

                string currentUser = _userContextProvider.GetUserName();
                if (settings.Users.TryGetValue(currentUser, out UserBackupProfile? profile))
                    BackupPassword = profile.BackupPassword;
                else
                    BackupPassword = GenerateRandomPassword();

                Log.Logger?.Debug("Backup UI properties updated on UI thread");
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading backup settings from {ConfigPath}", _configPath);
            App.Current.CurrentWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                SetDefaultBackupPath();
                BackupPassword = GenerateRandomPassword();
                RetentionDays = 7;
                Log.Logger?.Debug("Backup settings reset to defaults due to load error");
            });
        }
    }

    /// <summary>
    /// Opens folder picker for user to select custom backup destination path.
    /// </summary>
    private async Task BrowseBackupPathAsync()
    {
        Log.Logger?.Debug("BrowseBackupPathAsync: opening folder picker");

        var folderPicker = new FolderPicker();
        folderPicker.FileTypeFilter.Add("*");

        var window = App.Current.CurrentWindow;
        if (window == null)
        {
            Log.Logger?.Error("Current window is null, cannot initialize folder picker");
            return;
        }

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            BackupPath = folder.Path;
            Log.Logger?.Information("Backup path selected: {BackupPath}", folder.Path);
        }
        else
        {
            Log.Logger?.Debug("Folder picker cancelled by user");
        }
    }

    /// <summary>
    /// Resets backup path to the default Documents/Pango/Backup location.
    /// </summary>
    private void SetDefaultBackupPath()
    {
        BackupPath = GetDefaultPath();
        Log.Logger?.Debug("Backup path reset to default: {BackupPath}", BackupPath);
    }

    /// <summary>
    /// Opens the configured backup folder in File Explorer if it exists.
    /// </summary>
    private async Task OpenBackupFolderAsync()
    {
        Log.Logger?.Debug("OpenBackupFolderAsync: attempting to open {BackupPath}", BackupPath);

        if (Directory.Exists(BackupPath))
        {
            await Launcher.LaunchFolderPathAsync(BackupPath);
            Log.Logger?.Information("Backup folder opened in File Explorer");
        }
        else
        {
            Log.Logger?.Warning("Backup folder does not exist: {BackupPath}", BackupPath);
            SendNotification("BackupPathInvalid_Error", AppNotificationType.Warning);
        }
    }

    /// <summary>
    /// Saves current backup configuration to JSON file and optionally shows notification.
    /// </summary>
    /// <param name="showNotification">Whether to display success/error notification to user.</param>
    private async Task SaveBackupSettingsAsync(bool showNotification = true)
    {
        Log.Logger?.Information("SaveBackupSettingsAsync: saving backup configuration");

        if (string.IsNullOrWhiteSpace(BackupPath))
        {
            Log.Logger?.Warning("Cannot save backup settings: backup path is empty");
            if (showNotification) SendNotification("BackupPathInvalid_Error", AppNotificationType.Error);
            return;
        }

        try
        {
            if (!Directory.Exists(BackupPath))
            {
                Directory.CreateDirectory(BackupPath);
                Log.Logger?.Debug("Created backup directory: {BackupPath}", BackupPath);
            }

            BackupSettings? settings;
            if (File.Exists(_configPath))
            {
                string json = await File.ReadAllTextAsync(_configPath);
                settings = JsonSerializer.Deserialize<BackupSettings>(json) ?? new BackupSettings();
            }
            else
            {
                settings = new BackupSettings();
                Log.Logger?.Debug("Creating new backup settings file");
            }

            settings.TargetFolderPath = BackupPath;
            settings.IntervalMinutes = SelectedBackupInterval;
            settings.IsEnabled = IsBackupServiceEnabled;
            settings.RetentionDays = RetentionDays;
            settings.Users ??= [];

            string userName = _userContextProvider.GetUserName();
            var userProfile = new UserBackupProfile
            {
                BackupPassword = BackupPassword,
                SourceDataPath = _appDomainProvider.GetUserFolderPath(userName)
            };

            if (!settings.Users.TryAdd(userName, userProfile))
                settings.Users[userName] = userProfile;

            string newJson = JsonSerializer.Serialize(settings);
            await File.WriteAllTextAsync(_configPath, newJson);

            Log.Logger?.Information("Backup settings saved successfully for user: {UserName}", userName);

            if (showNotification)
            {
                WeakReferenceMessenger.Default.Send(new BackupStateChangedMessage(IsBackupServiceEnabled));
                SendNotification("BackupSaved_Message", AppNotificationType.Success);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save backup settings to {ConfigPath}", _configPath);
            if (showNotification)
                WeakReferenceMessenger.Default.Send(new InAppNotificationMessage($"Error: {ex.Message}", AppNotificationType.Error));
        }
    }

    /// <summary>
    /// Sends an in-app notification message using the specified resource key and type.
    /// </summary>
    /// <param name="resourceKey">Resource loader key for the notification message.</param>
    /// <param name="type">Notification severity type for styling.</param>
    private void SendNotification(string resourceKey, AppNotificationType type)
    {
        Log.Logger?.Debug("Sending notification: {ResourceKey} ({Type})", resourceKey, type);
        WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(_resourceLoader.GetString(resourceKey), type));
    }

    /// <summary>
    /// Returns the default backup path located in user's Documents folder.
    /// </summary>
    /// <returns>Full path to default backup directory.</returns>
    private static string GetDefaultPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pango", "Backup");

    /// <summary>
    /// Generates a random 12-character uppercase password for backup encryption.
    /// </summary>
    /// <returns>Randomly generated password string.</returns>
    private static string GenerateRandomPassword() => Guid.NewGuid().ToString("N")[..12].ToUpper();

    /// <summary>
    /// Loads the user's preferred import destination setting from local storage.
    /// </summary>
    /// <returns>ImportDestination enum value or Root as default.</returns>
    private static ImportDestination LoadImportSetting()
    {
        var val = ApplicationData.Current.LocalSettings.Values["ImportDestination"] as int?;
        return (ImportDestination)(val ?? 0);
    }

    /// <summary>
    /// Saves the user's import destination preference to local storage.
    /// </summary>
    /// <param name="value">The ImportDestination value to persist.</param>
    private static void SaveImportSetting(ImportDestination value)
    {
        ApplicationData.Current.LocalSettings.Values["ImportDestination"] = (int)value;
        Log.Logger?.Debug("Import destination setting saved: {Value}", value);
    }

    #endregion
}
