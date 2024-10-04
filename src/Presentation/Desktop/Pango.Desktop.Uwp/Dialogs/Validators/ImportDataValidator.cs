using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pango.Desktop.Uwp.Dialogs.Validators;

public class ImportDataValidator : ObservableValidator
{
    private string _masterPassword = string.Empty;

    public ImportDataValidator()
    {
        ValidateAllProperties();
    }

    [Required]
    [MinLength(3)]
    public string MasterPassword
    {
        get => _masterPassword;
        set
        {
            SetProperty(ref _masterPassword, value);
            ValidateProperty(value, nameof(MasterPassword));
        }
    }
}
