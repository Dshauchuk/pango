namespace Pango.Application.Common;

public class UserBackupProfile
{
    public string BackupPassword { get; set; } = string.Empty;
    public string SourceDataPath { get; set; } = string.Empty;
}

public class BackupSettings
{
    // Constants for default configuration values
    public const int DefaultIntervalMinutes = 60;
    public const int DefaultRetentionDays = 7;
    public const bool DefaultIsEnabled = true;

    public string TargetFolderPath { get; set; } = string.Empty;
    public int IntervalMinutes { get; set; } = DefaultIntervalMinutes;
    public int RetentionDays { get; set; } = DefaultRetentionDays;
    public bool IsEnabled { get; set; } = DefaultIsEnabled;
    public Dictionary<string, UserBackupProfile> Users { get; set; } = [];
}
