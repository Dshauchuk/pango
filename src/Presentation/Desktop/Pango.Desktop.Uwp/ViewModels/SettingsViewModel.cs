using CommunityToolkit.Mvvm.ComponentModel;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Pango.Desktop.Uwp.ViewModels;

public class SettingsViewModel : ObservableObject, IViewModel
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

    public async Task OnNavigatedFromAsync(object parameter)
    {
        await Task.CompletedTask;
    }

    public async Task OnNavigatedToAsync(object parameter)
    {
        await Task.CompletedTask;
    }
}
