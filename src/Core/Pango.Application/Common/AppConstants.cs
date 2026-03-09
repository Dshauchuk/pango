namespace Pango.Application.Common;

public static class AppConstants
{
    /// <summary>
    /// Presents a default number of items stored in each file
    /// </summary>
    public const int DefaultNumberOfItemsPerFile = 20;

    public const char CatalogDelimeter = '/';
    /// <summary>
    /// File extension used for exported data files.
    /// </summary>
    public const string ExportFileExtension = ".pngx";
    /// <summary>
    /// Default name of the subfolder used to store exported files
    /// under the user's Documents directory.
    /// </summary>
    public const string DefaultExportFolderName = "PangoExports";

    public const string UsersFolderName = "users";
}
