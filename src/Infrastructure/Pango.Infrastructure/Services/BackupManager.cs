using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using System.Globalization;
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
    public async Task PerformBackupForUserAsync(string userId, string sourcePath, string targetRootPath, string backupPassword, int retentionDays)
    {
        try
        {
            // Ensure the target directory exists
            if (!Directory.Exists(targetRootPath)) Directory.CreateDirectory(targetRootPath);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"{timestamp}-{userId}_Backup.pngx";
            string targetFilePath = Path.Combine(targetRootPath, fileName);
            string tempZipPath = targetFilePath + ".tmp";

            await Task.Run(() =>
            {
                if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
                ZipFile.CreateFromDirectory(sourcePath, tempZipPath);
            });

            await EncryptFileAsync(tempZipPath, targetFilePath, backupPassword);

            if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            _logger.LogInformation("Backup created: {Path}", targetFilePath);

            await CleanUpOldBackupsAsync(targetRootPath, userId, retentionDays);
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogWarning("Source directory not found for user {User}", userId);
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
        using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(salt); }

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

    public Task CleanUpOldBackupsAsync(string targetPath, string userName)
    {
        return CleanUpOldBackupsAsync(targetPath, userName, 7);
    }

    public Task CleanUpOldBackupsAsync(string targetPath, string userName, int retentionDays)
    {
        return Task.Run(() =>
        {
            try
            {
                if (retentionDays <= 0)
                {
                    _logger.LogDebug("Retention policy disabled (days <= 0). Keeping all files.");
                    return;
                }

                DirectoryInfo dir = new(targetPath);
                if (!dir.Exists) return;

                var files = dir.GetFiles($"*-{userName}_Backup.pngx");
                DateTime now = DateTime.Now;

                foreach (var file in files)
                {
                    if (file.Name.Length >= 19)
                    {
                        string datePart = file.Name[..19];
                        if (DateTime.TryParseExact(datePart, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime backupDate))
                        {
                            double ageInDays = (now - backupDate).TotalDays;

                            if (ageInDays > retentionDays)
                            {
                                try
                                {
                                    file.Delete();
                                    _logger.LogDebug("Deleted expired backup: {Name} (Age: {Age:F1} days)", file.Name, ageInDays);
                                }
                                catch (Exception delEx)
                                {
                                    _logger.LogWarning(delEx, "Failed to delete old backup {Name}", file.Name);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during retention cleanup");
            }
        });
    }
}