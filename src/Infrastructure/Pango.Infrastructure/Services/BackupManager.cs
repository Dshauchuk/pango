using Microsoft.Extensions.Logging;
using Pango.Application.Common.Interfaces.Services;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Pango.Infrastructure.Services;

public class BackupManager(ILogger<BackupManager> logger) : IBackupManager
{
    /// <summary>
    /// The default number of days to retain backup files before deletion.
    /// </summary>
    private const int DefaultRetentionDays = 7;

    /// <summary>
    /// The length of the timestamp prefix in the backup filename (yyyy-MM-dd_HH-mm-ss).
    /// </summary>
    private const int TimestampPrefixLength = 19;

    /// <summary>
    /// The date format string used for timestamping backup files.
    /// </summary>
    private const string DateFormat = "yyyy-MM-dd_HH-mm-ss";

    private readonly ILogger<BackupManager> _logger = logger;

    /// <summary>
    /// Orchestrates the backup process: zips, encrypts, and rotates old files.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="sourcePath">The directory path to back up.</param>
    /// <param name="targetRootPath">The destination directory for the backup.</param>
    /// <param name="backupPassword">The password used for encryption.</param>
    /// <param name="retentionDays">Number of days to keep old backups.</param>
    public async Task PerformBackupForUserAsync(string userId, string sourcePath, string targetRootPath, string backupPassword, int retentionDays)
    {
        try
        {
            // Ensure the target directory exists
            if (!Directory.Exists(targetRootPath)) Directory.CreateDirectory(targetRootPath);

            string fileName = GenerateBackupFileName(userId);
            string targetFilePath = Path.Combine(targetRootPath, fileName);
            string tempZipPath = targetFilePath + ".tmp";

            if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            await Task.Run(() => ZipFile.CreateFromDirectory(sourcePath, tempZipPath));

            await EncryptFileAsync(tempZipPath, targetFilePath, backupPassword);

            if (File.Exists(tempZipPath)) File.Delete(tempZipPath);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Backup created: {Path}", targetFilePath);
            }

            await CleanUpOldBackupsAsync(targetRootPath, userId, retentionDays);
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Source directory not found for user {User}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed for user {User}", userId);
        }
    }

    /// <summary>
    /// Generates a unique filename based on current timestamp and user ID.
    /// </summary>
    private static string GenerateBackupFileName(string userId)
    {
        string timestamp = DateTime.Now.ToString(DateFormat);
        return $"{timestamp}-{userId}_Backup.pngx";
    }

    /// <summary>
    /// Encrypts a file using AES-256 with Pbkdf2 key derivation.
    /// </summary>
    /// <param name="inputFile">The path to the source file (plaintext).</param>
    /// <param name="outputFile">The path to the destination file (encrypted).</param>
    /// <param name="password">The password used to derive the encryption key.</param>
    private static async Task EncryptFileAsync(string inputFile, string outputFile, string password)
    {
        // 1. Generate random Salt (16 bytes)
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(salt); }

        // 2. Derive Key and IV from Password and Salt
        byte[] derivedBytes = await Task.Run(() =>
            Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 50000, HashAlgorithmName.SHA256, 48));

        using var aes = Aes.Create();
        aes.Key = derivedBytes[0..32];
        aes.IV = derivedBytes[32..48];
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        await using var fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

        // 3. Write Salt (16 bytes) to the beginning of the file
        await fsOutput.WriteAsync(salt.AsMemory(0, salt.Length));

        // 4. Write IV (16 bytes) to the file
        await fsOutput.WriteAsync(aes.IV.AsMemory(0, aes.IV.Length));

        // 5. Write Encrypted Data
        await using var cs = new CryptoStream(fsOutput, aes.CreateEncryptor(), CryptoStreamMode.Write);
        await using var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        await fsInput.CopyToAsync(cs);
    }

    /// <summary>
    /// Overload for cleanup using the default retention period.
    /// </summary>
    /// <param name="targetPath">The directory containing backups.</param>
    /// <param name="userName">The user identifier to filter files.</param>
    public Task CleanUpOldBackupsAsync(string targetPath, string userName)
    {
        return CleanUpOldBackupsAsync(targetPath, userName, DefaultRetentionDays);
    }

    /// <summary>
    /// Deletes backup files older than the specified retention days.
    /// </summary>
    /// <param name="targetPath">The directory containing backups.</param>
    /// <param name="userName">The user identifier to filter files.</param>
    /// <param name="retentionDays">Number of days to keep old backups.</param>
    public async Task CleanUpOldBackupsAsync(string targetPath, string userName, int retentionDays)
    {
        await Task.Run(() =>
        {
            try
            {
                if (retentionDays <= 0)
                {
                    _logger.LogDebug("Retention policy disabled. Keeping all files.");
                    return;
                }

                DirectoryInfo dir = new(targetPath);
                if (!dir.Exists) return;

                var files = dir.GetFiles($"*-{userName}_Backup.pngx");
                DateTime now = DateTime.Now;

                foreach (var file in files)
                {
                    if (file.Name.Length >= TimestampPrefixLength)
                    {
                        string datePart = file.Name[..TimestampPrefixLength];
                        if (DateTime.TryParseExact(datePart, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime backupDate))
                        {
                            if (Math.Abs((file.CreationTime - backupDate).TotalMinutes) > 2)
                            {
                                _logger.LogWarning("Skipping cleanup for {Name}: Metadata timestamp does not match filename.", file.Name);
                                continue;
                            }

                            double ageInDays = (now - backupDate).TotalDays;

                            if (ageInDays > retentionDays)
                            {
                                try
                                {
                                    file.Delete();
                                    if (_logger.IsEnabled(LogLevel.Debug))
                                    {
                                        _logger.LogDebug("Deleted expired backup: {Name} (Age: {Age:F1} days)", file.Name, ageInDays);
                                    }
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