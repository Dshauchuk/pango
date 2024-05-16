using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel.DataAnnotations;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.ViewModels.Validators;

public class EditPasswordValidator : ObservableValidator
{
    private string _login;
    private string _password;
    private string _title;
    private string _selectedCatalog;
    private string _notes;
    private Guid? _id;

    private ResourceLoader _viewResourceLoader;

    public EditPasswordValidator()
    {
        _viewResourceLoader = ResourceLoader.GetForCurrentView();
    }

    public Guid? Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    [Required()]
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

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public string SelectedCatalog
    {
        get => _selectedCatalog;
        set => SetProperty(ref _selectedCatalog, value);
    }

    public void Validate()
    {
        ValidateAllProperties();
    }
}
