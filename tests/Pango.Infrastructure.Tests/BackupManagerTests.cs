using Microsoft.Extensions.Logging.Abstractions;
using Pango.Infrastructure.Services;

namespace Pango.Infrastructure.Tests;

public class BackupManagerTests
{
    [Fact]
    public async Task PerformBackupForUserAsync_CreatesEncryptedFile_FromSourceDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), "pango_backup_test_" + Guid.NewGuid().ToString("N"));
        string source = Path.Combine(root, "src");
        string target = Path.Combine(root, "tgt");
        Directory.CreateDirectory(source);
        await File.WriteAllTextAsync(Path.Combine(source, "sample.txt"), "hello");

        try
        {
            var mgr = new BackupManager(NullLogger<BackupManager>.Instance);
            await mgr.PerformBackupForUserAsync("user1", source, target, "backup-secret!", 7);

            var files = Directory.GetFiles(target, "*.pngx");
            Assert.Single(files);
            await using var fs = File.OpenRead(files[0]);
            Assert.True(fs.Length > 32);
            Assert.False(File.Exists(files[0] + ".tmp"));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task PerformBackupForUserAsync_Swallows_WhenSourceMissing()
    {
        string root = Path.Combine(Path.GetTempPath(), "pango_backup_missing_" + Guid.NewGuid().ToString("N"));
        string target = Path.Combine(root, "tgt");
        Directory.CreateDirectory(target);

        try
        {
            var mgr = new BackupManager(NullLogger<BackupManager>.Instance);
            await mgr.PerformBackupForUserAsync("u", Path.Combine(root, "nope"), target, "pw", 7);

            Assert.Empty(Directory.GetFiles(target, "*.pngx"));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task CleanUpOldBackupsAsync_DoesNothing_WhenRetentionNotPositive()
    {
        string root = Path.Combine(Path.GetTempPath(), "pango_retention_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-u_backup.pngx");
        await File.WriteAllTextAsync(path, "x");

        try
        {
            var mgr = new BackupManager(NullLogger<BackupManager>.Instance);
            await mgr.CleanUpOldBackupsAsync(root, "u", 0);

            Assert.True(File.Exists(path));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}
