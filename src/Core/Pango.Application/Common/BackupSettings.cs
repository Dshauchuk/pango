namespace Pango.Application.Common;

public class UserBackupProfile
{
    public string BackupPassword { get; set; } = string.Empty;
    public string SourceDataPath { get; set; } = string.Empty;
}

public class BackupSettings
{
    public string TargetFolderPath { get; set; } = string.Empty;
    public int IntervalMinutes { get; set; } = 60;
    public int RetentionDays { get; set; } = 7;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, UserBackupProfile> Users { get; set; } = [];
}