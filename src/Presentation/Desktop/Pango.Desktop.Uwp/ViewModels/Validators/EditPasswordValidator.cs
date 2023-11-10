using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.ViewModels.Validators;

public class EditPasswordValidator : ObservableValidator
{
    private string _login;
    private string _password;
    private string _title;
    private ResourceLoader _viewResourceLoader;

    public EditPasswordValidator()
    {
        _viewResourceLoader = ResourceLoader.GetForCurrentView();
    }

    [Required(AllowEmptyStrings = false, ErrorMessageResourceName = "Back", ErrorMessageResourceType = typeof(ResourceLoader))]
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);  
    }

    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }
}
