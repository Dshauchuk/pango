using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Pango.Persistence.File;

public class PangoFileDataImporter(
    IContentEncoder contentEncoder,
    IAppDomainProvider appDomainProvider,
    IUserContextProvider userContextProvider,
    ILogger<PangoFileDataImporter> logger) : IDataImporter
{
    private readonly IContentEncoder _contentEncoder = contentEncoder;
    private readonly IAppDomainProvider _appDomainProvider = appDomainProvider;
    private readonly IUserContextProvider _userContextProvider = userContextProvider;
    private readonly ILogger _logger = logger;

    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private const int MaxRetries = 3;
    private const int DelayMilliseconds = 1000;
    private const string PackageFileExtension = ".pngx";
    private const string DataFileExtension = ".pngdat";
    private const string StaticSaltStr = "PangoStaticExportSalt";

    /// <summary>
    /// Orchestrates the import process by securing access and reading data.
    /// </summary>
    public async Task<ImportResultDto> ImportAsync(string filePath, IImportOptions importOptions)
    {
        await _semaphore.WaitAsync();
        try
        {
            var manifest = await ReadManifestAsync(filePath, importOptions);
            var content = await ExtractContentAsync(filePath, importOptions);
            return new ImportResultDto(manifest, content);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Reads and decrypts the package manifest from the zip archive.
    /// </summary>
    public async Task<PangoPackageManifest> ReadManifestAsync(string filePath, IImportOptions importOptions)
    {
        ThrowIfFileIsNotValid(filePath);
        bool isEncryptedBackup = await IsEncryptedBackupFileAsync(filePath);
        string workingFilePath = filePath;
        string tempDecryptedPath = string.Empty;

        try
        {
            if (isEncryptedBackup)
            {
                _logger.LogInformation("File appears encrypted. Attempting backup decryption...");
                tempDecryptedPath = Path.GetTempFileName();
                string backupPassword = importOptions.EncodingOptions.Key;
                await DecryptBackupFileAsync(filePath, tempDecryptedPath, backupPassword);
                workingFilePath = tempDecryptedPath;
            }

            EncodingOptions userOptions = await _userContextProvider.GetEncodingOptionsAsync();

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Reading manifest from {filePath}", workingFilePath);
                    }

                    using var fs = new FileStream(workingFilePath, FileMode.Open);
                    using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

                    foreach (var entry in archive.Entries)
                    {
                        if (ShouldSkipEntry(entry)) continue;

                        using var entryStream = entry.Open();
                        using var ms = new MemoryStream();
                        await entryStream.CopyToAsync(ms);
                        byte[] encryptedData = ms.ToArray();

                        var decryptionOptions = isEncryptedBackup ? userOptions : importOptions.EncodingOptions;
                        var manifest = await TryDecrypt<PangoPackageManifest>(encryptedData, decryptionOptions);

                        if (!manifest.Equals(default))
                        {
                            return manifest;
                        }
                    }

                    return new PangoPackageManifest(
                        _userContextProvider.GetUserName(),
                        DateTime.UtcNow.ToString("G"),
                        "Restored from Backup",
                        []
                    );
                }
                catch (PangoDataDecryptionException ex)
                {
                    _logger.LogError(ex, "Failed to decrypt manifest data.");
                    throw;
                }
                catch (IOException e)
                {
                    _logger.LogError("Manifest reading attempt {Attempt} failed: {Message}", attempt + 1, e.Message);
                    if (attempt == MaxRetries - 1) throw;
                    await Task.Delay(DelayMilliseconds);
                }
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempDecryptedPath) && System.IO.File.Exists(tempDecryptedPath))
            {
                try
                {
                    System.IO.File.Delete(tempDecryptedPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary decrypted file at {Path}", tempDecryptedPath);
                }
            }
        }

        throw new PangoDataDecryptionException(ApplicationErrors.Data.DecryptionError, "Unable to read manifest from file.");
    }

    /// <summary>
    /// Extracts and decrypts content packages from the zip archive.
    /// </summary>
    public async Task<List<IContentPackage>> ExtractContentAsync(string filePath, IImportOptions importOptions)
    {
        ThrowIfFileIsNotValid(filePath);
        List<IContentPackage> importedPackages = [];
        bool isEncryptedBackup = await IsEncryptedBackupFileAsync(filePath);
        string workingFilePath = filePath;
        string tempDecryptedPath = string.Empty;

        try
        {
            if (isEncryptedBackup)
            {
                _logger.LogInformation("File appears encrypted. Attempting backup decryption...");
                tempDecryptedPath = Path.GetTempFileName();
                string backupPassword = importOptions.EncodingOptions.Key;
                await DecryptBackupFileAsync(filePath, tempDecryptedPath, backupPassword);
                workingFilePath = tempDecryptedPath;
            }

            EncodingOptions userOptions = await _userContextProvider.GetEncodingOptionsAsync();

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Extracting content from {filePath}", workingFilePath);
                    }

                    using var fs = new FileStream(workingFilePath, FileMode.Open);
                    using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

                    foreach (var entry in archive.Entries)
                    {
                        if (ShouldSkipEntry(entry)) continue;

                        using var entryStream = entry.Open();
                        using var ms = new MemoryStream();
                        await entryStream.CopyToAsync(ms);
                        byte[] encryptedData = ms.ToArray();

                        var decryptionOptions = isEncryptedBackup ? userOptions : importOptions.EncodingOptions;
                        IContentPackage? data = await TryDecrypt<ContentPackage>(encryptedData, decryptionOptions);

                        if (data == null && !isEncryptedBackup)
                        {
                            data = await TryDecrypt<ContentPackage>(encryptedData, userOptions);
                        }

                        if (data != null)
                        {
                            importedPackages.Add(data);
                        }
                    }
                    break;
                }
                catch (PangoDataDecryptionException ex)
                {
                    _logger.LogError(ex, "Critical decryption failure during content extraction.");
                    throw;
                }
                catch (IOException e)
                {
                    _logger.LogError("Content extraction attempt {Attempt} failed: {Message}", attempt + 1, e.Message);
                    if (attempt == MaxRetries - 1) throw;
                    await Task.Delay(DelayMilliseconds);
                }
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempDecryptedPath) && System.IO.File.Exists(tempDecryptedPath))
            {
                try
                {
                    System.IO.File.Delete(tempDecryptedPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary content file at {Path}", tempDecryptedPath);
                }
            }
            _logger.LogDebug("Content extraction completed");
        }

        return importedPackages;
    }

    /// <summary>
    /// Helper to filter out system files and non-data entries from the archive.
    /// </summary>
    private static bool ShouldSkipEntry(ZipArchiveEntry entry)
    {
        return string.IsNullOrEmpty(entry.Name) ||
               entry.FullName.StartsWith("_rels") ||
               entry.Name == "[Content_Types].xml" ||
               !entry.Name.EndsWith(DataFileExtension, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the file is a standard Zip or an encrypted backup.
    /// </summary>
    private static async Task<bool> IsEncryptedBackupFileAsync(string filePath)
    {
        try
        {
            using (ZipFile.OpenRead(filePath)) { }
            return await Task.FromResult(false);
        }
        catch (InvalidDataException) { return true; }
        catch (Exception) { return true; }
    }

    /// <summary>
    /// Attempts to decrypt raw byte data into a strongly typed object.
    /// </summary>
    private async Task<T?> TryDecrypt<T>(byte[] data, EncodingOptions options)
    {
        if (string.IsNullOrEmpty(options.Key)) return default;

        string keyBase64 = options.Key;
        string saltBase64 = options.Salt;

        Span<byte> buffer = new(new byte[100]);
        bool isValidKey = Convert.TryFromBase64String(options.Key, buffer, out int bytesWritten) && bytesWritten == 32;

        if (!isValidKey)
        {
            byte[] staticSalt = Encoding.UTF8.GetBytes(StaticSaltStr);

            byte[] derivedBytes = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(options.Key), staticSalt, 50000, HashAlgorithmName.SHA256, 48);

            keyBase64 = Convert.ToBase64String(derivedBytes[0..32]);
            saltBase64 = Convert.ToBase64String(derivedBytes[32..48]);
        }

        try
        {
            return await _contentEncoder.DecryptAsync<T>(data, keyBase64, saltBase64);
        }
        catch (PangoDataDecryptionException ex)
        {
            _logger.LogError(ex, "Decryption failed for internal data block.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Decryption of an entry failed: {Message}", ex.Message);
            return default;
        }
    }

    /// <summary>
    /// Decrypts the entire backup file using AES-256 and writes to output.
    /// </summary>
    private static async Task DecryptBackupFileAsync(string inputFile, string outputFile, string password)
    {
        await using var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        byte[] salt = new byte[16];
        if (await fsInput.ReadAsync(salt.AsMemory(0, 16)) < 16) throw new IOException("Invalid header");

        byte[] iv = new byte[16];
        if (await fsInput.ReadAsync(iv.AsMemory(0, 16)) < 16) throw new IOException("Invalid header");

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 50000, HashAlgorithmName.SHA256, 32);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Padding = PaddingMode.PKCS7;

        await using var fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await using var cs = new CryptoStream(fsInput, aes.CreateDecryptor(), CryptoStreamMode.Read);
        try
        {
            await cs.CopyToAsync(fsOutput);
        }
        catch (CryptographicException ex)
        {
            throw new PangoDataDecryptionException(ApplicationErrors.Data.DecryptionError, "Invalid backup password.", ex);
        }
    }

    /// <summary>
    /// Validates file existence and extension.
    /// </summary>
    private static void ThrowIfFileIsNotValid(string filePath)
    {
        if (!System.IO.File.Exists(filePath)) throw new PangoImportException($"File {filePath} doesn't exist");
        if (Path.GetExtension(filePath) != PackageFileExtension) throw new PangoImportException($"Invalid file extension");
    }
}
