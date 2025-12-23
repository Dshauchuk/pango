using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.Dialogs.Validators;

/// <summary>
/// Validator for export data fields, ensuring required inputs and validity.
/// </summary>
public partial class ExportDataValidator : ObservableValidator
{
    private static readonly ResourceLoader _resourceLoader = new();

    private const string FileExtension = ".pngx";

    private string _description = string.Empty;
    private string _masterPassword = string.Empty;
    private string _fileName = string.Empty;
    private string _exportFolderPath = string.Empty;

    // Constructor initializes and validates all properties
    public ExportDataValidator()
    {
        ValidateAllProperties();
    }

    // Property for description with custom validation
    [CustomValidation(typeof(ExportDataValidator), nameof(ValidateDescription))]
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value, true);
    }

    // Property for master password with custom validation
    [CustomValidation(typeof(ExportDataValidator), nameof(ValidatePassword))]
    public string MasterPassword
    {
        get => _masterPassword;
        set => SetProperty(ref _masterPassword, value, true);
    }

    // Property for file name with custom validation
    [CustomValidation(typeof(ExportDataValidator), nameof(ValidateFileName))]
    public string FileName
    {
        get => _fileName;
        set
        {
            SetProperty(ref _fileName, value, true);
        }
    }

    // Property for export folder path; triggers FileName re-validation on change
    [CustomValidation(typeof(ExportDataValidator), nameof(ValidateExportPath))]
    public string ExportFolderPath
    {
        get => _exportFolderPath;
        set
        {
            if (SetProperty(ref _exportFolderPath, value, true))
            {
                ValidateProperty(FileName, nameof(FileName));
            }
        }
    }

    // Validates description: required and minimum length of 3
    public static ValidationResult? ValidateDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new ValidationResult(_resourceLoader.GetString("ValidationError_Required"));
        if (value.Length < 3) return new ValidationResult(_resourceLoader.GetString("ValidationError_MinLength"));
        return ValidationResult.Success;
    }

    // Validates password: required and minimum length of 3
    public static ValidationResult? ValidatePassword(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new ValidationResult(_resourceLoader.GetString("ValidationError_Required"));
        if (value.Length < 3) return new ValidationResult(_resourceLoader.GetString("ValidationError_MinLength"));
        return ValidationResult.Success;
    }

    // Validates file name: required, no invalid chars, and checks for file existence if path exists
    public static ValidationResult? ValidateFileName(string value, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ValidationResult(_resourceLoader.GetString("ValidationError_Required"));
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        if (value.IndexOfAny(invalidChars) >= 0)
        {
            return new ValidationResult(_resourceLoader.GetString("ValidationError_InvalidFileName"));
        }

        var validator = (ExportDataValidator)context.ObjectInstance;
        if (!string.IsNullOrWhiteSpace(validator.ExportFolderPath) && Directory.Exists(validator.ExportFolderPath))
        {
            string fullPath = Path.Combine(validator.ExportFolderPath, $"{value}{FileExtension}");
            if (File.Exists(fullPath))
            {
                return new ValidationResult(_resourceLoader.GetString("ValidationError_FileExists"));
            }
        }

        return ValidationResult.Success;
    }

    // Validates export path: required and must be an existing directory
    public static ValidationResult? ValidateExportPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return new ValidationResult(_resourceLoader.GetString("ValidationError_ExportPath"));
        }
        return ValidationResult.Success;
    }

    // Manually triggers validation for all properties
    public void Validate()
    {
        ValidateAllProperties();
    }

    // Resets properties to empty and re-validates
    public void Reset()
    {
        Description = string.Empty;
        MasterPassword = string.Empty;

        ValidateAllProperties();
    }
}
