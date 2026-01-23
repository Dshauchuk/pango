using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using Pango.Desktop.Uwp.Core;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Utility;
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
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.Settings)]
public partial class SettingsViewModel : ViewModelBase
{
    #region Fields

    // Services
    private readonly IUserContextProvider _userContextProvider;
    private readonly IAppDomainProvider _appDomainProvider;

    // Appearance & Behavior
    private AppLanguage _selectedLanguage;
    private AppTheme _selectedAppTheme;
    private bool _allowAutolock;
    private KeyValuePair<int, string>? _selectedLockOnIdleInMinutesItem;

    // Backup Configuration
    private string _configPath = string.Empty;
    private string _backupPath = string.Empty;
    private int _selectedBackupInterval;
    private bool _isBackupServiceEnabled;
    private string _backupPassword = string.Empty;

    #endregion

    #region Constructor

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        IUserContextProvider userContextProvider,
        IAppDomainProvider appDomainProvider) : base(logger)
    {
        _userContextProvider = userContextProvider;
        _appDomainProvider = appDomainProvider;

        // 1. Initialize Collections
        Languages = new ObservableCollection<AppLanguage>(AppLanguage.GetAppLanguageCollection());
        AppThemes = new ObservableCollection<AppTheme>(Enum.GetValues(typeof(ElementTheme))
            .Cast<ElementTheme>()
            .Select(e => new AppTheme { Name = ViewResourceLoader.GetString($"AppTheme_{e}"), Value = (int)e }));

        string localizedMinutes = ViewResourceLoader.GetString("Minute(-s)");
        LockOnIdleInMinutesItems = new ObservableCollection<KeyValuePair<int, string>>(
        [
            new(1, $"1 {localizedMinutes}"),
            new(3, $"3 {localizedMinutes}"),
            new(5, $"5 {localizedMinutes}"),
            new(10, $"10 {localizedMinutes}"),
            new(15, $"15 {localizedMinutes}"),
            new(30, $"30 {localizedMinutes}")
        ]);

        BackupIntervals = new ObservableCollection<int>([1, 5, 10, 15, 20, 30, 45, 60]);

        // 2. Initialize App Settings (Language/Theme)
        string appliedLocale = AppLanguageHelper.GetAppliedAppLanguage().Locale;
        _selectedLanguage = Languages.FirstOrDefault(e => e.Locale == appliedLocale) ?? Languages.First();
        _selectedAppTheme = AppThemes.First(e => e.Value == (int)AppThemeHelper.Theme);

        // 3. Initialize Autolock Settings
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

        // 4. Initialize Backup Configuration
        _ = InitializeBackupConfigAsync();
    }

    #endregion

    #region Properties - General

    public static string Version
    {
        get
        {
            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
            return version is null ? "undefined" : string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }
    }

    #endregion

    #region Properties - Appearance & Language

    public ObservableCollection<AppLanguage> Languages { get; private set; }

    public AppLanguage SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (value is not null)
            {
                AppLanguageHelper.ChangeAppLanguage(value, typeof(SettingsView));
                SetProperty(ref _selectedLanguage, value);
            }
        }
    }

    public ObservableCollection<AppTheme> AppThemes { get; private set; }

    public AppTheme SelectedAppTheme
    {
        get => _selectedAppTheme;
        set
        {
            if (value != null)
            {
                ElementTheme? elementTheme = (ElementTheme)value.Value;
                if (elementTheme.HasValue && elementTheme.Value != AppThemeHelper.Theme)
                {
                    AppThemeHelper.SetTheme(elementTheme.Value);
                }
                SetProperty(ref _selectedAppTheme, value);
            }
        }
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
            SelectedLockOnIdleInMinutesItem = value ? LockOnIdleInMinutesItems[3] : null;
            SetProperty(ref _allowAutolock, value);
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

    #endregion

    #region Methods - Backup Implementation

    private async Task InitializeBackupConfigAsync()
    {
        // Path to ProgramData/Pango/backup_config.json
        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string configDir = Path.Combine(commonAppData, "Pango");

        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
        }

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

            string currentUser = _userContextProvider.GetUserName();
            bool needToSaveImmediately = false;

            if (settings.Users.TryGetValue(currentUser, out UserBackupProfile? profile))
            {
                BackupPassword = profile.BackupPassword;
            }
            else
            {
                BackupPassword = GenerateRandomPassword();

                string sourcePath = _appDomainProvider.GetUserFolderPath(currentUser);

                settings.Users.Add(currentUser, new UserBackupProfile
                {
                    BackupPassword = BackupPassword,
                    SourceDataPath = sourcePath
                });

                needToSaveImmediately = true;
            }

            if (needToSaveImmediately)
            {
                await SaveSettingsToFileAsync(settings);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading backup settings");
            SetDefaultBackupPath();
            BackupPassword = GenerateRandomPassword();
        }
    }

    private async Task SaveSettingsToFileAsync(BackupSettings settings)
    {
        string json = JsonSerializer.Serialize(settings);
        await File.WriteAllTextAsync(_configPath, json);
    }

    private static string GetDefaultPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pango", "Backup");
    }

    private async Task BrowseBackupPathAsync()
    {
        FolderPicker folderPicker = new();
        folderPicker.FileTypeFilter.Add("*");

        var window = App.Current.CurrentWindow;
        if (window == null) return;

        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

        StorageFolder folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            BackupPath = folder.Path;
        }
    }

    private void SetDefaultBackupPath()
    {
        string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pango", "Backup");
        BackupPath = defaultPath;
    }

    private async Task OpenBackupFolderAsync()
    {
        if (Directory.Exists(BackupPath))
        {
            await Launcher.LaunchFolderPathAsync(BackupPath);
        }
    }

    private async Task SaveBackupSettingsAsync()
    {
        if (string.IsNullOrWhiteSpace(BackupPath) || BackupPath.Length < 3 || Path.GetInvalidPathChars().Any(BackupPath.Contains))
        {
            SendNotification("BackupPathInvalid_Error", AppNotificationType.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(BackupPassword) || BackupPassword.Length < 3)
        {
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                "Password must be at least 3 characters long",
                AppNotificationType.Error));
            return;
        }

        try
        {
            if (!Directory.Exists(BackupPath))
            {
                Directory.CreateDirectory(BackupPath);
            }

            BackupSettings settings;
            if (File.Exists(_configPath))
            {
                string existingJson = await File.ReadAllTextAsync(_configPath);
                settings = JsonSerializer.Deserialize<BackupSettings>(existingJson) ?? new BackupSettings();
            }
            else
            {
                settings = new BackupSettings();
            }

            settings.Users ??= [];

            settings.TargetFolderPath = BackupPath;
            settings.IntervalMinutes = SelectedBackupInterval;
            settings.IsEnabled = IsBackupServiceEnabled;

            string userName = _userContextProvider.GetUserName();
            string absoluteSourcePath = _appDomainProvider.GetUserFolderPath(userName);

            var userProfile = new UserBackupProfile
            {
                BackupPassword = this.BackupPassword,
                SourceDataPath = absoluteSourcePath
            };

            if (!settings.Users.TryAdd(userName, userProfile))
            {
                settings.Users[userName] = userProfile;
            }

            string json = JsonSerializer.Serialize(settings);
            await File.WriteAllTextAsync(_configPath, json);

            SendNotification("BackupSaved_Message", AppNotificationType.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save backup settings");
            WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                $"Error: {ex.Message}",
                AppNotificationType.Error));
        }
    }

    private void SendNotification(string resourceKey, AppNotificationType type)
    {
        WeakReferenceMessenger.Default.Send(new InAppNotificationMessage(
                ViewResourceLoader.GetString(resourceKey), type));
    }

    private static string GenerateRandomPassword()
    {
        return Guid.NewGuid().ToString("N")[..12].ToUpper();
    }

    #endregion
}
