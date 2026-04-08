using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.Settings)]
public partial class SettingsViewModel : ViewModelBase
{
    #region Nested Types

    public enum ImportDestination { Root = 0, Folder = 1 }

    public class ImportDestinationOption
    {
        public required string Content { get; set; }
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
    private bool _isChangingDataFolder;
    private string _dataFolderProgressText = string.Empty;

    // Backup Configuration
    private string _configPath = string.Empty;
    private string _backupPath = string.Empty;
    private int _selectedBackupInterval;
    private int _retentionDays;
    private bool _isBackupServiceEnabled;
    private string _backupPassword = string.Empty;

    #endregion

    #region Constructor

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

        Languages = new ObservableCollection<AppLanguage>(AppLanguage.GetAppLanguageCollection());
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
        else
        {
            _selectedLockOnIdleInMinutesItem = null;
            _allowAutolock = false;
        }

        _ = InitializeBackupConfigAsync();
        _ = CheckStartupStatusAsync();
    }

    #endregion

    #region Properties - General

    public string Version => _appMetaService.GetAppVersion();

    #endregion

    #region Properties - Appearance & Language

    public ObservableCollection<AppLanguage> Languages { get; private set; }

    public AppLanguage SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (value is not null && SetProperty(ref _selectedLanguage, value))
            {
                AppLanguageHelper.ChangeAppLanguage(value, typeof(SettingsView));
                InitializeDisplayResources();
            }
        }
    }

    public ObservableCollection<AppTheme> AppThemes { get; private set; }

    public AppTheme SelectedAppTheme
    {
        get => _selectedAppTheme;
        set
        {
            if (value != null && SetProperty(ref _selectedAppTheme, value))
            {
                AppThemeHelper.SetTheme((ElementTheme)value.Value);
            }
        }
    }

    #endregion

    #region Properties - Data Storage
    public string SelectedDataFolderPath
    {
        get => _selectedDataFolderPath;
        set => SetProperty(ref _selectedDataFolderPath, value);
    }
    public bool IsChangingDataFolder
    {
        get => _isChangingDataFolder;
        set => SetProperty(ref _isChangingDataFolder, value);
    }
    public string DataFolderProgressText
    {
        get => _dataFolderProgressText;
        set => SetProperty(ref _dataFolderProgressText, value);
    }
    #endregion

    #region Properties - Security (Autolock)

    public ObservableCollection<KeyValuePair<int, string>> LockOnIdleInMinutesItems { get; private set; }

    public KeyValuePair<int, string>? SelectedLockOnIdleInMinutesItem
    {
        get => _selectedLockOnIdleInMinutesItem;
        set
        {
            ApplicationData.Current.LocalSettings.Values[Constants.Settings.BlockAppAfterIdleMinutes] = value?.Key;
            WeakReferenceMessenger.Default.Send<AutolockIdleChangedMessage>(new(value?.Key));
            SetProperty(ref _selectedLockOnIdleInMinutesItem, value);
        }
    }

    public bool AllowAutolock
    {
        get => _allowAutolock;
        set
        {
            if (SetProperty(ref _allowAutolock, value))
            {
                if (value && SelectedLockOnIdleInMinutesItem == null && LockOnIdleInMinutesItems.Count > 2)
                    SelectedLockOnIdleInMinutesItem = LockOnIdleInMinutesItems[2];

                if (!value)
                    SelectedLockOnIdleInMinutesItem = null;
            }
        }
    }

    public bool AllowLaunchAtStartup
    {
        get => _allowLaunchAtStartup;
        set
        {
            if (SetProperty(ref _allowLaunchAtStartup, value))
            {
                _ = ToggleStartupAsync(value);
            }
        }
    }

    #endregion

    #region Properties - Backup

    public ObservableCollection<int> BackupIntervals { get; }

    public string BackupPath
    {
        get => _backupPath;
        set => SetProperty(ref _backupPath, value);
    }

    public int SelectedBackupInterval
    {
        get => _selectedBackupInterval;
        set => SetProperty(ref _selectedBackupInterval, value);
    }

    public int RetentionDays
    {
        get => _retentionDays;
        set
        {
            if (value < 1) value = 1;
            if (value > 365) value = 365;
            SetProperty(ref _retentionDays, value);
        }
    }

    public bool IsBackupServiceEnabled
    {
        get => _isBackupServiceEnabled;
        set => SetProperty(ref _isBackupServiceEnabled, value);
    }

    public string BackupPassword
    {
        get => _backupPassword;
        set => SetProperty(ref _backupPassword, value);
    }

    public ObservableCollection<ImportDestinationOption> ImportDestinationOptions
    {
        get => _importDestinationOptions;
        private set => SetProperty(ref _importDestinationOptions, value);
    }

    public ImportDestinationOption? SelectedImportDestination
    {
        get => ImportDestinationOptions.FirstOrDefault(x => x.Value == _selectedImportDestination);
        set
        {
            if (value != null && SetProperty(ref _selectedImportDestination, value.Value))
            {
                SaveImportSetting(value.Value);
            }
        }
    }

    #endregion

    #region Commands
    private RelayCommand? _selectDataFolderCommand;
    public RelayCommand SelectDataFolderCommand => _selectDataFolderCommand ??= new RelayCommand(async () => await SelectDataFolderAsync());

    private RelayCommand? _resetDataFolderCommand;
    public RelayCommand ResetDataFolderCommand => _resetDataFolderCommand ??= new RelayCommand(ResetDataFolder);

    #endregion


    #region Commands - Backup

    private RelayCommand? _browseBackupPathCommand;
    public RelayCommand BrowseBackupPathCommand => _browseBackupPathCommand ??= new RelayCommand(async () => await BrowseBackupPathAsync());

    private RelayCommand? _setDefaultBackupPathCommand;
    public RelayCommand SetDefaultBackupPathCommand => _setDefaultBackupPathCommand ??= new RelayCommand(SetDefaultBackupPath);

    private RelayCommand? _openBackupFolderCommand;
    public RelayCommand OpenBackupFolderCommand => _openBackupFolderCommand ??= new RelayCommand(async () => await OpenBackupFolderAsync());

    private RelayCommand? _saveBackupSettingsCommand;
    public RelayCommand SaveBackupSettingsCommand => _saveBackupSettingsCommand ??= new RelayCommand(async () => await SaveBackupSettingsAsync());

    private RelayCommand? _cancelBackupChangesCommand;
    public RelayCommand CancelBackupChangesCommand => _cancelBackupChangesCommand ??= new RelayCommand(async () => await LoadBackupSettingsAsync());

    public void BugRequestCard_Click(object _, RoutedEventArgs _1)
    {
        _ = Launcher.LaunchUriAsync(new Uri("https://github.com/"));
    }

    #endregion

    #region Methods - Startup Implementation 

    private async Task CheckStartupStatusAsync()
    {
        bool enabled = await _startupService.IsStartupEnabledAsync();
        SetProperty(ref _allowLaunchAtStartup, enabled, nameof(AllowLaunchAtStartup));
    }

    private async Task ToggleStartupAsync(bool enable)
    {
        if (enable)
        {
            bool success = await _startupService.EnableStartupAsync();
            if (!success)
                SetProperty(ref _allowLaunchAtStartup, false, nameof(AllowLaunchAtStartup));
        }
        else
        {
            await _startupService.DisableStartupAsync();
        }
    }

    #endregion

    #region Methods - Select data folder
    private async Task SelectDataFolderAsync()
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop
        };
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.CurrentWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        StorageFolder folder = await picker.PickSingleFolderAsync();

        if (folder == null) return;

        bool confirmed = await ConfirmAsync(
            ViewResourceLoader.GetString("ChangeDataFolder"),
            ViewResourceLoader.GetString("ChangeDataFolder_Confirmation"));
        if (!confirmed) return;

        string oldPath = _appDomainProvider.GetAppDataFolderPath();
        string newPath = folder.Path;

        if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            IsChangingDataFolder = true;
            DataFolderProgressText = ViewResourceLoader.GetString("CopyingData");

            // migrate app's data
            await Task.Run(() => _userStorageManager.MigrateDataAsync(oldPath, newPath));

            DataFolderProgressText = ViewResourceLoader.GetString("UpdatingConfiguration");

            // save token
            StorageApplicationPermissions.FutureAccessList.AddOrReplace(
                Constants.Settings.CustomDataFolderToken, folder);

            // update cash
            await _appDomainProvider.TryGetCustomDataFolderPathAsync();
            
            SelectedDataFolderPath = folder.Path;

            WeakReferenceMessenger.Default.Send(
                new InAppNotificationMessage(
                    ViewResourceLoader.GetString("DataFolderChanged"),
                    AppNotificationType.Success));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to change data folder: {Message}", ex.Message);
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(ViewResourceLoader.GetString("DataFolderChangeFailed"), AppNotificationType.Warning));
        }
        finally
        {
            IsChangingDataFolder = false;
            DataFolderProgressText = string.Empty;
        }
    }

    private void ResetDataFolder()
    {
        // remove the custom folder and the default folder will return
        if (StorageApplicationPermissions.FutureAccessList.ContainsItem(
                Constants.Settings.CustomDataFolderToken))
        {
            StorageApplicationPermissions.FutureAccessList.Remove(
                Constants.Settings.CustomDataFolderToken);
        }

        SelectedDataFolderPath = ApplicationData.Current.RoamingFolder.Path;

        WeakReferenceMessenger.Default.Send(
        new InAppNotificationMessage(
            ViewResourceLoader.GetString("DataFolderChanged"),
            AppNotificationType.Success));
    }

    #endregion

    #region Methods - Backup Implementation

    private void InitializeDisplayResources()
    {
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
        {
            _selectedLockOnIdleInMinutesItem = LockOnIdleInMinutesItems.FirstOrDefault(k => k.Key == currentIdleKey);
        }
        OnPropertyChanged(nameof(SelectedLockOnIdleInMinutesItem));

        // 3. Refresh Import Options
        var currentImportVal = _selectedImportDestination;

        ImportDestinationOptions.Clear();
        ImportDestinationOptions.Add(new() { Content = _resourceLoader.GetString("ImportDestination_Root"), Value = ImportDestination.Root });
        ImportDestinationOptions.Add(new() { Content = _resourceLoader.GetString("ImportDestination_Folder"), Value = ImportDestination.Folder });

        // Restore selected item based on saved Value
        OnPropertyChanged(nameof(SelectedImportDestination));
    }

    private async Task InitializeBackupConfigAsync()
    {
        // Path to ProgramData/Pango/backup_config.json
        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string configDir = Path.Combine(commonAppData, "Pango");
        if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);

        _configPath = Path.Combine(configDir, "backup_config.json");
        await LoadBackupSettingsAsync();
    }

    private async Task LoadBackupSettingsAsync()
    {
        try
        {
            BackupSettings? settings = null;
            if (File.Exists(_configPath))
            {
                string json = await File.ReadAllTextAsync(_configPath);
                settings = JsonSerializer.Deserialize<BackupSettings>(json);
            }

            settings ??= new BackupSettings();
            settings.Users ??= [];

            BackupPath = string.IsNullOrEmpty(settings.TargetFolderPath) ? GetDefaultPath() : settings.TargetFolderPath;
            SelectedBackupInterval = settings.IntervalMinutes > 0 ? settings.IntervalMinutes : 60;
            IsBackupServiceEnabled = settings.IsEnabled;
            RetentionDays = settings.RetentionDays > 0 ? settings.RetentionDays : 7;

            string currentUser = _userContextProvider.GetUserName();

            if (settings.Users.TryGetValue(currentUser, out UserBackupProfile? profile))
            {
                BackupPassword = profile.BackupPassword;
            }
            else
            {
                BackupPassword = GenerateRandomPassword();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading backup settings");
            SetDefaultBackupPath();
            BackupPassword = GenerateRandomPassword();
            RetentionDays = 7;
        }
    }

    private async Task SaveSettingsToFileAsync(BackupSettings settings)
    {
        string json = JsonSerializer.Serialize(settings);
        await File.WriteAllTextAsync(_configPath, json);
    }

    private static string GetDefaultPath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pango", "Backup");

    private async Task BrowseBackupPathAsync()
    {
        FolderPicker folderPicker = new();
        folderPicker.FileTypeFilter.Add("*");

        var window = App.Current.CurrentWindow;
        if (window == null) return;

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

        StorageFolder folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null) BackupPath = folder.Path;
    }

    private void SetDefaultBackupPath() => BackupPath = GetDefaultPath();

    private async Task OpenBackupFolderAsync()
    {
        if (Directory.Exists(BackupPath))
        {
            await Launcher.LaunchFolderPathAsync(BackupPath);
        }
        else
        {
            SendNotification("BackupPathInvalid_Error", AppNotificationType.Warning);
        }
    }

    private async Task SaveBackupSettingsAsync()
    {
        if (string.IsNullOrWhiteSpace(BackupPath))
        {
            SendNotification("BackupPathInvalid_Error", AppNotificationType.Error);
            return;
        }

        try
        {
            if (!Directory.Exists(BackupPath)) Directory.CreateDirectory(BackupPath);

            BackupSettings? settings;
            if (File.Exists(_configPath))
            {
                string json = await File.ReadAllTextAsync(_configPath);
                settings = JsonSerializer.Deserialize<BackupSettings>(json) ?? new BackupSettings();
            }
            else
            {
                settings = new BackupSettings();
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

            await SaveSettingsToFileAsync(settings);
            SendNotification("BackupSaved_Message", AppNotificationType.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save backup settings");
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage($"Error: {ex.Message}", AppNotificationType.Error));
        }
    }

    private void SendNotification(string resourceKey, AppNotificationType type)
    {
        WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(_resourceLoader.GetString(resourceKey), type));
    }

    private static string GenerateRandomPassword() => Guid.NewGuid().ToString("N")[..12].ToUpper();

    private static ImportDestination LoadImportSetting()
    {
        var val = ApplicationData.Current.LocalSettings.Values["ImportDestination"] as int?;
        return (ImportDestination)(val ?? 0);
    }

    private static void SaveImportSetting(ImportDestination value)
    {
        ApplicationData.Current.LocalSettings.Values["ImportDestination"] = (int)value;
    }

    #endregion
}
