using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel.DataAnnotations;

namespace Pango.Desktop.Uwp.ViewModels.Validators;

public partial class EditPasswordValidator : ObservableValidator
{
    private string _login = string.Empty;
    private string _password = string.Empty;
    private string _title = string.Empty;
    private string? _selectedCatalog;
    private string _notes = string.Empty;
    private bool _isStar = false;
    private Guid? _id;
    private DateTimeOffset? _expirationDate = DateTimeOffset.Now;
    private bool _hasExpirationDate;

    public EditPasswordValidator()
    {
    }

    public DateTimeOffset? ExpirationDate
    {
        get => _expirationDate;
        set => SetProperty(ref _expirationDate, value);
    }

    public bool HasExpirationDate
    {
        get => _hasExpirationDate;
        set
        {
            SetProperty(ref _hasExpirationDate, value);
            if (value && !ExpirationDate.HasValue)
                ExpirationDate = DateTimeOffset.Now;
            else if (!value)
                ExpirationDate = null;
        }
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

    public string? SelectedCatalog
    {
        get => _selectedCatalog;
        set => SetProperty(ref _selectedCatalog, value);
    }
    public bool Star
    {
        get => _isStar;
        set => SetProperty(ref _isStar, value);
    }

    public void Validate()
    {
        ValidateAllProperties();
    }
}
