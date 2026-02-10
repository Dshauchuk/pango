using Microsoft.Extensions.Logging;
using Moq;
using Pango.Infrastructure.Services;
using System.Runtime.Versioning;

namespace Pango.Tests;

[SupportedOSPlatform("windows")]
public class BackupRotationTests : IDisposable
{
    private readonly string _testBackupFolder;
    private readonly Mock<ILogger<BackupManager>> _mockLogger;
    private readonly BackupManager _backupManager;
    private const string TestUser = "testuser";

    // Sets up the test environment and creates a temp directory
    public BackupRotationTests()
    {
        _testBackupFolder = Path.Combine(Path.GetTempPath(), "PangoBackupTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testBackupFolder);

        _mockLogger = new Mock<ILogger<BackupManager>>();
        _backupManager = new BackupManager(_mockLogger.Object);
    }

    // Cleans up the temp directory after tests
    public void Dispose()
    {
        if (Directory.Exists(_testBackupFolder))
        {
            try { Directory.Delete(_testBackupFolder, true); } catch { }
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    // Verifies that files older than the retention period are deleted
    public async Task CleanUpOldBackupsAsync_ShouldDeleteFiles_OlderThanRetentionDays()
    {
        // Arrange
        int retentionDays = 7;
        string oldFile = CreateDummyBackupFile(DateTime.Now.AddDays(-10)); // 10 days old
        string newFile = CreateDummyBackupFile(DateTime.Now.AddDays(-2));  // 2 days old
        string todayFile = CreateDummyBackupFile(DateTime.Now);

        // Act
        await _backupManager.CleanUpOldBackupsAsync(_testBackupFolder, TestUser, retentionDays);

        // Assert
        Assert.False(File.Exists(oldFile), "Old file (> 7 days) should be deleted");
        Assert.True(File.Exists(newFile), "New file (2 days) should exist");
        Assert.True(File.Exists(todayFile), "Today's file should exist");
    }

    [Fact]
    // Verifies that no files are deleted if infinite retention (0) is selected
    public async Task CleanUpOldBackupsAsync_ShouldKeepAll_WhenRetentionIsZero()
    {
        // Arrange
        int retentionDays = 0; // Infinite
        string veryOldFile = CreateDummyBackupFile(DateTime.Now.AddDays(-365));

        // Act
        await _backupManager.CleanUpOldBackupsAsync(_testBackupFolder, TestUser, retentionDays);

        // Assert
        Assert.True(File.Exists(veryOldFile), "File should be kept when retention is 0 (infinite)");
    }

    [Fact]
    // Verifies that files within the valid date range are preserved
    public async Task CleanUpOldBackupsAsync_ShouldNotDelete_IfWithinRetentionPeriod()
    {
        // Arrange
        int retentionDays = 30;
        string file = CreateDummyBackupFile(DateTime.Now.AddDays(-20));

        // Act
        await _backupManager.CleanUpOldBackupsAsync(_testBackupFolder, TestUser, retentionDays);

        // Assert
        Assert.True(File.Exists(file), "File within retention period should not be deleted");
    }

    [Fact]
    // Verifies that random files or files from other users are ignored
    public async Task CleanUpOldBackupsAsync_ShouldIgnore_NonBackupFiles()
    {
        // Arrange
        string randomFile = Path.Combine(_testBackupFolder, "random.txt");
        File.WriteAllText(randomFile, "data");

        string wrongPattern = Path.Combine(_testBackupFolder, "2020-01-01-wronguser_Backup.pngx");
        File.WriteAllText(wrongPattern, "data");

        // Act
        await _backupManager.CleanUpOldBackupsAsync(_testBackupFolder, TestUser, 5);

        // Assert
        Assert.True(File.Exists(randomFile), "Random text file should be ignored");
        Assert.True(File.Exists(wrongPattern), "File with wrong user pattern should be ignored");
    }

    [Fact]
    // Verifies behavior when file age exactly matches retention limit
    public async Task CleanUpOldBackupsAsync_ShouldHandle_EdgeCase_ExactlyOnRetentionLimit()
    {
        // Arrange
        int retentionDays = 5;
        // 5 days minus 1 minute means it is NOT YET older than 5 days
        string edgeFile = CreateDummyBackupFile(DateTime.Now.AddDays(-5).AddMinutes(1));

        // Act
        await _backupManager.CleanUpOldBackupsAsync(_testBackupFolder, TestUser, retentionDays);

        // Assert
        Assert.True(File.Exists(edgeFile), "File slightly inside retention limit should exist");
    }

    // Helper method to create a dummy file with a specific date in filename
    private string CreateDummyBackupFile(DateTime date)
    {
        string timestamp = date.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{timestamp}-{TestUser}_Backup.pngx";
        string fullPath = Path.Combine(_testBackupFolder, fileName);

        File.WriteAllText(fullPath, "dummy encrypted content");

        // Update OS timestamps to match filename for consistency
        File.SetCreationTime(fullPath, date);
        File.SetLastWriteTime(fullPath, date);

        return fullPath;
    }
}