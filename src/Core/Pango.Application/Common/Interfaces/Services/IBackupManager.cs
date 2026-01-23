namespace Pango.Application.Common.Interfaces.Services;

// Defines the contract for backup operations
public interface IBackupManager
{
    Task PerformBackupAsync(BackupSettings settings);
    Task PerformBackupForUserAsync(string userId, string sourcePath, string targetRootPath, string backupPassword);
    Task CleanUpOldBackupsAsync(string targetPath, string userName);
}