using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pango.Desktop.Uwp.Dialogs.Validators;

public class ExportDataValidator : ObservableValidator
{
    private string _description;
    private string _masterPassword;

    public ExportDataValidator()
    {
        _description = string.Empty;
        _masterPassword = string.Empty;

        ValidateAllProperties();
    }

    [Required]
    [MinLength(3)]
    [MaxLength(20)]
    public string Description
    {
        get => _description;
        set
        {
            SetProperty(ref _description, value);
            ValidateProperty(value, nameof(Description));
        }
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
