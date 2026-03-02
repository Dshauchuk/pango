namespace Pango.Application.Common.Interfaces.Services;

// Defines the contract for backup operations
public interface IBackupManager
{
    Task PerformBackupForUserAsync(string userId, string sourcePath, string targetRootPath, string backupPassword, int retentionDays);

    Task CleanUpOldBackupsAsync(string targetPath, string userName);
}