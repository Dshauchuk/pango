using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Pango.Desktop.Uwp.Core;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.Settings)]
public class SettingsViewModel : ViewModelBase
{
    #region Fields

    private AppLanguage _selectedLanguage;
    private AppTheme _selectedAppTheme;
    private bool _allowAutolock;
    private KeyValuePair<int, string>?  _selectedLockOnIdleInMinutesItem;

    #endregion

    public SettingsViewModel(ILogger<SettingsViewModel> logger) : base(logger)
    {
        Languages = new ObservableCollection<AppLanguage>(AppLanguage.GetAppLanguageCollection());
        AppThemes = new ObservableCollection<AppTheme>(Enum.GetValues(typeof(ElementTheme)).Cast<ElementTheme>().Select(e => new AppTheme { Name = ViewResourceLoader.GetString($"AppTheme_{e}"), Value = (int)e }));
        
        string localizedMinutes = ViewResourceLoader.GetString("Minute(-s)");
        LockOnIdleInMinutesItems = new ObservableCollection<KeyValuePair<int, string>>(new List<KeyValuePair<int, string>>
        {
            new(1, $"1 {localizedMinutes}"),
            new(3, $"3 {localizedMinutes}"),
            new(5, $"5 {localizedMinutes}"),
            new(10, $"10 {localizedMinutes}"),
            new(15, $"15 {localizedMinutes}"),
            new(30, $"30 {localizedMinutes}")
        });

        string appliedLocale = AppLanguageHelper.GetAppliedAppLanguage().Locale;
        _selectedLanguage = Languages.FirstOrDefault(e => e.Locale == appliedLocale) ?? Languages.First();
        _selectedAppTheme = AppThemes.First(e => e.Value == (int)AppThemeHelper.Theme);

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
    }

    #region Properties

    public static string Version
    {
        get
        {
            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
            return version is null ? "undefined" : string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }
    }

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
            if(value != null)
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
}
