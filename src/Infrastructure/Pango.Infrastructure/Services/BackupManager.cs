using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Pango.Infrastructure.Services;

public class BackupManager(ILogger<BackupManager> logger) : IBackupManager
{
    private readonly ILogger<BackupManager> _logger = logger;

    // Legacy method required by interface, currently unused
    public async Task PerformBackupAsync(BackupSettings settings)
    {
        if (string.IsNullOrEmpty(settings.TargetFolderPath)) return;
        await Task.CompletedTask;
    }

    // Main logic: Rotates files, Zips content, Encrypts Zip, and Cleans up old files
    public async Task PerformBackupForUserAsync(string userId, string sourcePath, string targetRootPath, string backupPassword)
    {
        try
        {
            // Ensure the target directory exists
            if (!Directory.Exists(targetRootPath)) Directory.CreateDirectory(targetRootPath);

            string datePart = DateTime.Now.ToString("yyyy-MM-dd");
            string baseFileName = $"{datePart}-{userId}_Backup";

            // Determine target file path based on rotation logic
            string targetFilePath = await Task.Run(() => RotateAndGetPath(targetRootPath, baseFileName));
            string tempZipPath = targetFilePath + ".tmp";

            await Task.Run(() =>
            {
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                if (File.Exists(targetFilePath)) File.Delete(targetFilePath);

                ZipFile.CreateFromDirectory(sourcePath, tempZipPath);
            });

            await EncryptFileAsync(tempZipPath, targetFilePath, backupPassword);

            File.Delete(tempZipPath);

            _logger.LogInformation("Encrypted backup created for {User} at {Path}", userId, targetFilePath);

            // Remove older backups from previous days
            await CleanUpOldBackupsAsync(targetRootPath, userId);
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogWarning("Source directory for user {User} not found: {Path}", userId, sourcePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed for user {User}", userId);
        }
    }

    // Encrypts file using AES-256 with a random Salt and IV stored in the file header
    private static async Task EncryptFileAsync(string inputFile, string outputFile, string password)
    {
        // 1. Generate random Salt (16 bytes)
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // 2. Derive Key and IV from Password and Salt
        using var kdf = new Rfc2898DeriveBytes(password, salt, 50000, HashAlgorithmName.SHA256);
        byte[] key = kdf.GetBytes(32);
        byte[] iv = kdf.GetBytes(16);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        await using var fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

        // 3. Write Salt (16 bytes) to the beginning of the file
        await fsOutput.WriteAsync(salt.AsMemory(0, salt.Length));

        // 4. Write IV (16 bytes) to the file
        await fsOutput.WriteAsync(iv.AsMemory(0, iv.Length));

        // 5. Write Encrypted Data
        await using var cs = new CryptoStream(fsOutput, aes.CreateEncryptor(), CryptoStreamMode.Write);
        await using var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        await fsInput.CopyToAsync(cs);
    }

    // Rotates files to keep max 3 versions per day (Base -> 1 -> 2)
    private static string RotateAndGetPath(string folder, string baseName)
    {
        string file0 = Path.Combine(folder, $"{baseName}.pngx");
        string file1 = Path.Combine(folder, $"{baseName}1.pngx");
        string file2 = Path.Combine(folder, $"{baseName}2.pngx");

        // Shift existing files: 1->2, 0->1
        if (File.Exists(file1))
        {
            if (File.Exists(file2)) File.Delete(file2);
            File.Move(file1, file2);
        }

        if (File.Exists(file0))
        {
            File.Move(file0, file1);
        }

        return file0;
    }

    // Deletes files from previous days, keeping only the latest one per day
    public Task CleanUpOldBackupsAsync(string targetPath, string userName)
    {
        return Task.Run(() =>
        {
            try
            {
                DirectoryInfo dir = new(targetPath);
                if (!dir.Exists) return;

                string todayString = DateTime.Now.ToString("yyyy-MM-dd");

                // Find all backup files for this user
                var files = dir.GetFiles($"*-{userName}_Backup*.pngx");

                // Group by Date part of filename
                var groups = files.GroupBy(f => f.Name.Length >= 10 ? f.Name[..10] : "unknown");

                foreach (var group in groups)
                {
                    // Skip today's backups
                    if (group.Key == todayString) continue;

                    // Keep only the newest file in the group
                    var orderedFiles = group.OrderByDescending(f => f.LastWriteTime).ToList();

                    for (int i = 1; i < orderedFiles.Count; i++)
                    {
                        try
                        {
                            orderedFiles[i].Delete();
                            _logger.LogDebug("Deleted old backup: {Name}", orderedFiles[i].Name);
                        }
                        catch (Exception delEx)
                        {
                            _logger.LogWarning(delEx, "Failed to delete file {Name}", orderedFiles[i].Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during old backup cleanup");
            }
        });
    }
}