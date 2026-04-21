namespace Pango.Application.Common;

/// <summary>
/// A dedicated structure for managing paths within the application to reduce hardcoding and simplify directory resolution.
/// </summary>
public readonly struct AppPaths
{
    public static string CommonAppData => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Pango");

    public static string MyDocuments => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pango");

    public static string BackupConfigPath => Path.Combine(CommonAppData, "backup_config.json");

    public static string DefaultBackupTarget => Path.Combine(MyDocuments, "Backup");

    public static string GetUserFolder(string userName) => Path.Combine(CommonAppData, AppConstants.UsersFolderName, userName);
}