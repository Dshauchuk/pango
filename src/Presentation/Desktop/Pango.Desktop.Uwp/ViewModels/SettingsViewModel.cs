using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.Settings)]
public class SettingsViewModel : ViewModelBase
{
    #region Fields

    private AppLanguage _selectedLanguage;
    private AppTheme _selectedAppTheme;

    #endregion

    public SettingsViewModel(ILogger<SettingsViewModel> logger) : base(logger)
    {
        Languages = new ObservableCollection<AppLanguage>(AppLanguage.GetAppLanguageCollection());
        AppThemes = new ObservableCollection<AppTheme>(Enum.GetValues(typeof(ElementTheme)).Cast<ElementTheme>().Select(e => new AppTheme { Name = ViewResourceLoader.GetString($"AppTheme_{e}"), Value = (int)e }));

        _selectedLanguage = Languages.FirstOrDefault(e => e.Locale == AppLanguageHelper.GetAppliedAppLanguage().Locale) ?? Languages.First();
        _selectedAppTheme = AppThemes.First(e => e.Value == (int)AppThemeHelper.Theme);
    }

    #region Properties

    public ObservableCollection<AppLanguage> Languages { get; private set; }

    public AppLanguage SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (value is not null)
            {
                AppLanguageHelper.ChangeAppLanguage(value, typeof(SettingsView));
            }
            SetProperty(ref _selectedLanguage, value);
        }
    }

    public ObservableCollection<AppTheme> AppThemes { get; private set; }

    public AppTheme SelectedAppTheme
    {
        get => _selectedAppTheme;
        set
        {
            ElementTheme? elementTheme = (ElementTheme)value?.Value;
            if (elementTheme.HasValue && elementTheme.Value != AppThemeHelper.Theme)
            {
                AppThemeHelper.SetTheme(elementTheme.Value);
            }
            SetProperty(ref _selectedAppTheme, value);
        }
    }

    #endregion
}
