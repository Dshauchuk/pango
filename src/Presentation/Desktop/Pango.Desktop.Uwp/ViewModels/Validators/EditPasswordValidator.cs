using CommunityToolkit.Mvvm.ComponentModel;
using Pango.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Pango.Desktop.Uwp.ViewModels.Validators;

public partial class EditPasswordValidator : ObservableValidator, IDisposable
{
    private string _login = string.Empty;
    private string _title = string.Empty;
    private string? _selectedCatalog;
    private string _notes = string.Empty;
    private bool _isStar = false;
    private Guid? _id;
    private DateTimeOffset? _expirationDate = DateTimeOffset.Now;
    private bool _hasExpirationDate;

    private readonly RamProtectedString _protectedPassword = new(string.Empty);

    public EditPasswordValidator()
    {
    }

    public DateTimeOffset? ExpirationDate
    {
        get => _expirationDate;
        set => SetProperty(ref _expirationDate, value, validate: false);
    }

    public bool HasExpirationDate
    {
        get => _hasExpirationDate;
        set
        {
            if (SetProperty(ref _hasExpirationDate, value, validate: false))
            {
                if (value && !ExpirationDate.HasValue)
                {
                    ExpirationDate = DateTimeOffset.Now;
                }
            }
        }
    }

    public Guid? Id
    {
        get => _id;
        set => SetProperty(ref _id, value, validate: false);
    }

    [Required]
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value, validate: false);
    }

    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value, validate: false);
    }

    public string Password
    {
        get => _protectedPassword.GetDecryptedValue();
        set
        {
            _protectedPassword.SetPlaintextValue(value);
            OnPropertyChanged(nameof(Password));
        }
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value, validate: false);
    }

    public string? SelectedCatalog
    {
        get => _selectedCatalog;
        set => SetProperty(ref _selectedCatalog, value, validate: false);
    }

    public bool Star
    {
        get => _isStar;
        set => SetProperty(ref _isStar, value, validate: false);
    }

    public void Validate()
    {
        ValidateAllProperties();
    }

    public void ClearAllErrors()
    {
        ClearErrors();
    }

    public void Reset()
    {
        Id = null;
        Title = string.Empty;
        Login = string.Empty;
        Password = string.Empty;
        Notes = string.Empty;
        SelectedCatalog = null;
        Star = false;
        HasExpirationDate = false;
        ExpirationDate = null;
        ClearAllErrors();
    }

    public void Dispose()
    {
        _protectedPassword.Dispose();
    }
}
