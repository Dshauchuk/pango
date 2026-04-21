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
        string tempDecryptedPath = string.Empty;
        string workingFilePath = filePath;

        try
        {
            ThrowIfFileIsNotValid(filePath);
            bool isEncrypted = await IsEncryptedBackupFileAsync(filePath);

            if (isEncrypted)
            {
                _logger.LogInformation("File is encrypted. Decrypting once...");
                tempDecryptedPath = Path.Combine(_appDomainProvider.GetTempFolderPath(), Guid.NewGuid().ToString("N") + ".tmp");
                await DecryptBackupFileAsync(filePath, tempDecryptedPath, importOptions.EncodingOptions.Key);
                workingFilePath = tempDecryptedPath;
            }

            var manifest = await ReadManifestInternalAsync(workingFilePath, importOptions, isEncrypted);
            var content = await ExtractContentInternalAsync(workingFilePath, importOptions, isEncrypted);

            return new ImportResultDto(manifest, content);
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempDecryptedPath) && System.IO.File.Exists(tempDecryptedPath))
            {
                try { System.IO.File.Delete(tempDecryptedPath); } catch { }
            }
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Reads and decrypts the package manifest from the zip archive. (Standalone call)
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
                tempDecryptedPath = Path.Combine(_appDomainProvider.GetTempFolderPath(), Guid.NewGuid().ToString("N") + ".tmp");
                await DecryptBackupFileAsync(filePath, tempDecryptedPath, importOptions.EncodingOptions.Key);
                workingFilePath = tempDecryptedPath;
            }

            return await ReadManifestInternalAsync(workingFilePath, importOptions, isEncryptedBackup);
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempDecryptedPath) && System.IO.File.Exists(tempDecryptedPath))
            {
                try { System.IO.File.Delete(tempDecryptedPath); } catch { }
            }
        }
    }

    /// <summary>
    /// Extracts and decrypts content packages from the zip archive. (Standalone call)
    /// </summary>
    public async Task<List<IContentPackage>> ExtractContentAsync(string filePath, IImportOptions importOptions)
    {
        ThrowIfFileIsNotValid(filePath);
        bool isEncryptedBackup = await IsEncryptedBackupFileAsync(filePath);
        string workingFilePath = filePath;
        string tempDecryptedPath = string.Empty;

        try
        {
            if (isEncryptedBackup)
            {
                tempDecryptedPath = Path.Combine(_appDomainProvider.GetTempFolderPath(), Guid.NewGuid().ToString("N") + ".tmp");
                await DecryptBackupFileAsync(filePath, tempDecryptedPath, importOptions.EncodingOptions.Key);
                workingFilePath = tempDecryptedPath;
            }

            return await ExtractContentInternalAsync(workingFilePath, importOptions, isEncryptedBackup);
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempDecryptedPath) && System.IO.File.Exists(tempDecryptedPath))
            {
                try { System.IO.File.Delete(tempDecryptedPath); } catch { }
            }
        }
    }

    private async Task<PangoPackageManifest> ReadManifestInternalAsync(string workingFilePath, IImportOptions importOptions, bool isEncryptedBackup)
    {
        EncodingOptions userOptions = await _userContextProvider.GetEncodingOptionsAsync();

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Reading manifest from {filePath}", workingFilePath);

                await using var fs = new FileStream(workingFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
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
                            return manifest;
                    }
                }

                return new PangoPackageManifest(_userContextProvider.GetUserName(), DateTime.UtcNow.ToString("G"), "Restored from Backup", []);
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

        throw new PangoDataDecryptionException(ApplicationErrors.Data.DecryptionError, "Unable to read manifest from file.");
    }

    private async Task<List<IContentPackage>> ExtractContentInternalAsync(string workingFilePath, IImportOptions importOptions, bool isEncryptedBackup)
    {
        List<IContentPackage> importedPackages = [];
        EncodingOptions userOptions = await _userContextProvider.GetEncodingOptionsAsync();

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Extracting content from {filePath}", workingFilePath);

                var fs = new FileStream(workingFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
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
                            data = await TryDecrypt<ContentPackage>(encryptedData, userOptions);

                        if (data != null)
                            importedPackages.Add(data);
                    }
                }

                await fs.DisposeAsync();
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

        _logger.LogDebug("Content extraction completed");
        return importedPackages;
    }

    /// <summary>
    /// Helper to filter out system files and non-data entries from the archive.
    /// </summary>
    private static bool ShouldSkipEntry(ZipArchiveEntry entry)
        => string.IsNullOrEmpty(entry.Name) ||
           entry.FullName.StartsWith("_rels") ||
           entry.Name == "[Content_Types].xml" ||
           !entry.Name.EndsWith(DataFileExtension, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the file is a standard Zip or an encrypted backup.
    /// </summary>
    private static async Task<bool> IsEncryptedBackupFileAsync(string filePath)
    {
        int retries = 3;
        for (int i = 0; i < retries; i++)
        {
            try
            {
                await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
                byte[] header = new byte[4];
                int bytesRead = await fs.ReadAsync(header.AsMemory(0, 4));

                if (bytesRead < 4) return false;

                bool isZip = header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04;
                return !isZip;
            }
            catch (IOException)
            {
                if (i == retries - 1)
                    throw new PangoImportException($"Cannot access file '{filePath}'. It is currently locked by another process.");

                await Task.Delay(500);
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to decrypt raw byte data into a strongly typed object.
    /// </summary>
    private async Task<T?> TryDecrypt<T>(byte[] data, EncodingOptions options)
    {
        if (string.IsNullOrEmpty(options.Key)) return default;

        string keyBase64 = options.Key;
        string saltBase64 = options.Salt;

        Span<byte> buffer = stackalloc byte[100];
        bool isValidKey = Convert.TryFromBase64String(options.Key, buffer, out int bytesWritten) && bytesWritten == 32;

        if (!isValidKey)
        {
            byte[] derivedBytes = await Task.Run(() =>
            {
                byte[] staticSalt = Encoding.UTF8.GetBytes(StaticSaltStr);
                return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(options.Key), staticSalt, 50000, HashAlgorithmName.SHA256, 48);
            });
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
        await using var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
        byte[] salt = new byte[16];
        if (await fsInput.ReadAsync(salt.AsMemory(0, 16)) < 16) throw new IOException("Invalid header");

        byte[] iv = new byte[16];
        if (await fsInput.ReadAsync(iv.AsMemory(0, 16)) < 16) throw new IOException("Invalid header");

        byte[] key = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 50000, HashAlgorithmName.SHA256, 32);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Padding = PaddingMode.PKCS7;

        await using var fsOutput = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
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
