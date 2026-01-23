using Microsoft.Extensions.Logging;
using Moq;
using Pango.Infrastructure.Services;

namespace Pango.Tests.Infrastructure;

public class BackupRotationTests : IDisposable
{
    private readonly string _testTargetDir;
    private readonly string _testSourceDir;
    private readonly BackupManager _manager;
    private readonly Mock<ILogger<BackupManager>> _loggerMock;

    // Constructor acts as Setup in xUnit
    public BackupRotationTests()
    {
        _testTargetDir = Path.Combine(Path.GetTempPath(), "Pango_Target_" + Guid.NewGuid());
        _testSourceDir = Path.Combine(Path.GetTempPath(), "Pango_Source_" + Guid.NewGuid());

        Directory.CreateDirectory(_testTargetDir);
        Directory.CreateDirectory(_testSourceDir);

        // Create a dummy file in source to zip and encrypt
        File.WriteAllText(Path.Combine(_testSourceDir, "data.txt"), "Sensitive Content");

        _loggerMock = new Mock<ILogger<BackupManager>>();
        _manager = new BackupManager(_loggerMock.Object);
    }

    // Dispose acts as TearDown in xUnit
    public void Dispose()
    {
        if (Directory.Exists(_testTargetDir)) Directory.Delete(_testTargetDir, true);
        if (Directory.Exists(_testSourceDir)) Directory.Delete(_testSourceDir, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PerformBackupForUser_ShouldCreateEncryptedFile()
    {
        // Arrange
        string userId = "TestUser";
        string password = "TestPassword";

        // Act
        await _manager.PerformBackupForUserAsync(userId, _testSourceDir, _testTargetDir, password);

        // Assert
        string date = DateTime.Now.ToString("yyyy-MM-dd");
        string expectedFile = Path.Combine(_testTargetDir, $"{date}-{userId}_Backup.pngx");

        Assert.True(File.Exists(expectedFile));

        // Ensure file is not empty
        var fileInfo = new FileInfo(expectedFile);
        Assert.True(fileInfo.Length > 0);
    }

    [Fact]
    public async Task PerformBackupForUser_ShouldRotateMaxThreeFiles()
    {
        // Arrange
        string userId = "TestUser";
        string password = "TestPassword";
        string date = DateTime.Now.ToString("yyyy-MM-dd");

        string baseFile = Path.Combine(_testTargetDir, $"{date}-{userId}_Backup.pngx");
        string file1 = Path.Combine(_testTargetDir, $"{date}-{userId}_Backup1.pngx");
        string file2 = Path.Combine(_testTargetDir, $"{date}-{userId}_Backup2.pngx");

        // Act & Assert 1: First backup
        await _manager.PerformBackupForUserAsync(userId, _testSourceDir, _testTargetDir, password);
        Assert.True(File.Exists(baseFile));
        Assert.False(File.Exists(file1));

        // Act & Assert 2: Second backup (should move base to 1)
        await Task.Delay(50);
        await _manager.PerformBackupForUserAsync(userId, _testSourceDir, _testTargetDir, password);
        Assert.True(File.Exists(baseFile));
        Assert.True(File.Exists(file1));
        Assert.False(File.Exists(file2));

        // Act & Assert 3: Third backup (should fill 2)
        await Task.Delay(50);
        await _manager.PerformBackupForUserAsync(userId, _testSourceDir, _testTargetDir, password);
        Assert.True(File.Exists(baseFile));
        Assert.True(File.Exists(file1));
        Assert.True(File.Exists(file2));
    }

    [Fact]
    public async Task CleanUpOldBackups_ShouldKeepOnlyLatestOfYesterday()
    {
        // Arrange
        string userId = "CleanupUser";
        string oldDate = "2023-01-01";

        // Simulate 3 files from a past date
        string f1 = Path.Combine(_testTargetDir, $"{oldDate}-{userId}_Backup.pngx");
        string f2 = Path.Combine(_testTargetDir, $"{oldDate}-{userId}_Backup1.pngx");
        string f3 = Path.Combine(_testTargetDir, $"{oldDate}-{userId}_Backup2.pngx");

        File.Create(f1).Close();
        await Task.Delay(20);
        File.Create(f2).Close();
        await Task.Delay(20);
        File.Create(f3).Close();

        // Force WriteTime to verify logic relies on timestamp, not just name
        File.SetLastWriteTime(f1, DateTime.Now.AddDays(-10));
        File.SetLastWriteTime(f2, DateTime.Now.AddDays(-9));
        File.SetLastWriteTime(f3, DateTime.Now.AddDays(-8));

        // Act
        await _manager.CleanUpOldBackupsAsync(_testTargetDir, userId);

        // Assert
        var files = Directory.GetFiles(_testTargetDir);

        Assert.Single(files);
        Assert.Equal(f3, files[0]);
    }
}