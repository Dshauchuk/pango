using CommunityToolkit.Mvvm.ComponentModel;
using Pango.Application.Common;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Windows.ApplicationModel.Resources;

namespace Pango.Desktop.Uwp.Dialogs.Validators;

/// <summary>
/// Provides validation logic for export dialog fields: description, master password,
/// export file name and target folder.
/// </summary>
public partial class ExportDataValidator : ObservableValidator
{
    /// <summary>
    /// Shared resource loader used to obtain localized validation messages.
    /// </summary>
    private static readonly ResourceLoader _resourceLoader = new();

    /// <summary>
    /// Backing field for <see cref="Description"/>.
    /// </summary>
    private string _description = string.Empty;

    /// <summary>
    /// Backing field for <see cref="MasterPassword"/>.
    /// </summary>
    private string _masterPassword = string.Empty;

    /// <summary>
    /// Backing field for <see cref="FileName"/>.
    /// </summary>
    private string _fileName = string.Empty;

    /// <summary>
    /// Backing field for <see cref="ExportFolderPath"/>.
    /// </summary>
    private string _exportFolderPath = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportDataValidator"/> class
    /// and triggers initial validation for all properties.
    /// </summary>
    public ExportDataValidator()
    {
        ValidateAllProperties();
    }

    /// <summary>
    /// Gets or sets the description for the export operation.
    /// </summary>
    [CustomValidation(typeof(ExportDataValidator), nameof(ValidateDescription))]
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value, true);
    }

    /// <summary>
    /// Gets or sets the master password used to protect exported data.
    /// </summary>
    [CustomValidation(typeof(ExportDataValidator), nameof(ValidatePassword))]
    public string MasterPassword
    {
        get => _masterPassword;
        set => SetProperty(ref _masterPassword, value, true);
    }

    /// <summary>
    /// Gets or sets the name of the export file without extension.
    /// </summary>
    [CustomValidation(typeof(ExportDataValidator), nameof(ValidateFileName))]
    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value, true);
    }

    /// <summary>
    /// Gets or sets the directory path where the file will be exported.
    /// </summary>
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

    /// <summary>
    /// Validates the <see cref="Description"/> value: required and minimum length.
    /// </summary>
    public static ValidationResult? ValidateDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new ValidationResult(_resourceLoader.GetString("ValidationError_Required"));
        if (value.Length < PasswordConstants.MinLength)
            return new ValidationResult(_resourceLoader.GetString("ValidationError_MinLength"));

        return ValidationResult.Success;
    }

    /// <summary>
    /// Validates the <see cref="MasterPassword"/> value: required and minimum length.
    /// </summary>
    public static ValidationResult? ValidatePassword(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new ValidationResult(_resourceLoader.GetString("ValidationError_Required"));
        if (value.Length < 3)
            return new ValidationResult(_resourceLoader.GetString("ValidationError_MinLength"));

        return ValidationResult.Success;
    }

    /// <summary>
    /// Validates the <see cref="FileName"/>: required, contains no invalid characters
    /// and does not already exist in the selected <see cref="ExportFolderPath"/>.
    /// </summary>
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
        if (!string.IsNullOrWhiteSpace(validator.ExportFolderPath) &&
            Directory.Exists(validator.ExportFolderPath))
        {
            string fullPath = Path.Combine(validator.ExportFolderPath, $"{value}{AppConstants.ExportFileExtension}");
            if (File.Exists(fullPath))
            {
                return new ValidationResult(_resourceLoader.GetString("ValidationError_FileExists"));
            }
        }

        return ValidationResult.Success;
    }

    /// <summary>
    /// Validates the <see cref="ExportFolderPath"/>: must be a non-empty existing directory.
    /// </summary>
    public static ValidationResult? ValidateExportPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return new ValidationResult(_resourceLoader.GetString("ValidationError_ExportPath"));
        }

        return ValidationResult.Success;
    }

    /// <summary>
    /// Triggers validation for all decorated properties.
    /// </summary>
    public void Validate()
    {
        ValidateAllProperties();
    }

    /// <summary>
    /// Resets user-entered values and revalidates all properties.
    /// </summary>
    public void Reset()
    {
        Description = string.Empty;
        MasterPassword = string.Empty;

        ValidateAllProperties();
    }
}
