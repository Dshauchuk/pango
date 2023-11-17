using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Views;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.Settings)]
public class SettingsViewModel : ViewModelBase
{
    #region Fields

    private AppLanguage _selectedLanguage;

    #endregion

    public SettingsViewModel()
    {
        Languages = new ObservableCollection<AppLanguage>(AppLanguage.GetAppLanguageCollection());
        //TODO: initial value does not applied to the combobox. need to fix
        _selectedLanguage = AppLanguageHelper.GetAppliedAppLanguage() ?? Languages.First();
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

    #endregion
}
