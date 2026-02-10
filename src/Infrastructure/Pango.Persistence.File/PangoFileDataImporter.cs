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
    private const int DelayMiliseconds = 1000;
    private const string PackageFileExtension = ".pngx";

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
                    _logger.LogDebug("Reading manifest from {filePath}", workingFilePath);
                    using var fs = new FileStream(workingFilePath, FileMode.Open);
                    using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

                    foreach (var entry in archive.Entries)
                    {
                        // FIX: Ignore non-data files, system files, and folders
                        if (string.IsNullOrEmpty(entry.Name) ||
                            entry.FullName.StartsWith("_rels") ||
                            entry.Name == "[Content_Types].xml" ||
                            !entry.Name.EndsWith(".pngdat", StringComparison.OrdinalIgnoreCase))
                            continue;

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
                catch (PangoDataDecryptionException)
                {
                    throw;
                }
                catch (IOException e)
                {
                    _logger.LogError("Manifest reading attempt {attempt} failed: {message}", attempt + 1, e.Message);
                    if (attempt == MaxRetries - 1) throw;
                    await Task.Delay(DelayMiliseconds);
                }
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempDecryptedPath) && System.IO.File.Exists(tempDecryptedPath))
            {
                try { System.IO.File.Delete(tempDecryptedPath); } catch { }
            }
        }

        throw new PangoDataDecryptionException(ApplicationErrors.Data.DecryptionError, "Unable to read manifest from file.");
    }

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
                    _logger.LogDebug("Extracting content from {filePath}", workingFilePath);
                    using var fs = new FileStream(workingFilePath, FileMode.Open);
                    using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

                    foreach (var entry in archive.Entries)
                    {
                        // FIX: Strict filter for .pngdat files only
                        if (string.IsNullOrEmpty(entry.Name) ||
                            entry.FullName.StartsWith("_rels") ||
                            entry.Name == "[Content_Types].xml" ||
                            !entry.Name.EndsWith(".pngdat", StringComparison.OrdinalIgnoreCase))
                            continue;

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
                catch (PangoDataDecryptionException)
                {
                    throw;
                }
                catch (IOException e)
                {
                    _logger.LogError("Content extraction attempt {attempt} failed: {message}", attempt + 1, e.Message);
                    if (attempt == MaxRetries - 1) throw;
                    await Task.Delay(DelayMiliseconds);
                }
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempDecryptedPath) && System.IO.File.Exists(tempDecryptedPath))
            {
                try { System.IO.File.Delete(tempDecryptedPath); } catch { }
            }
            _logger.LogDebug("Content extraction completed");
        }

        return importedPackages;
    }

    private static async Task<bool> IsEncryptedBackupFileAsync(string filePath)
    {
        try
        {
            using (ZipFile.OpenRead(filePath)) { }
            return false;
        }
        catch (InvalidDataException) { return true; }
        catch (Exception) { return true; }
    }

    private async Task<T?> TryDecrypt<T>(byte[] data, EncodingOptions options)
    {
        if (string.IsNullOrEmpty(options.Key)) return default;

        string keyBase64 = options.Key;
        string saltBase64 = options.Salt;

        Span<byte> buffer = new(new byte[100]);
        bool isValidKey = Convert.TryFromBase64String(options.Key, buffer, out int bytesWritten) && bytesWritten == 32;

        if (!isValidKey)
        {
            byte[] staticSalt = Encoding.UTF8.GetBytes("PangoStaticExportSalt");
            using var derive = new Rfc2898DeriveBytes(options.Key, staticSalt, 50000, HashAlgorithmName.SHA256);
            keyBase64 = Convert.ToBase64String(derive.GetBytes(32));
            saltBase64 = Convert.ToBase64String(derive.GetBytes(16));
        }

        try
        {
            return await _contentEncoder.DecryptAsync<T>(data, keyBase64, saltBase64);
        }
        catch (PangoDataDecryptionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Decryption of an entry failed: {Message}", ex.Message);
            return default;
        }
    }

    private static async Task DecryptBackupFileAsync(string inputFile, string outputFile, string password)
    {
        await using var fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        byte[] salt = new byte[16];
        if (await fsInput.ReadAsync(salt.AsMemory(0, 16)) < 16) throw new IOException("Invalid header");

        byte[] iv = new byte[16];
        if (await fsInput.ReadAsync(iv.AsMemory(0, 16)) < 16) throw new IOException("Invalid header");

        using var kdf = new Rfc2898DeriveBytes(password, salt, 50000, HashAlgorithmName.SHA256);
        byte[] key = kdf.GetBytes(32);

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
    /// Throws <see cref="PangoImportException"/> if file <paramref name="filePath"/> is not valid
    /// </summary>
    /// <param name="filePath"></param>
    /// <exception cref="PangoImportException"></exception>
    private static void ThrowIfFileIsNotValid(string filePath)
    {
        if (!System.IO.File.Exists(filePath)) throw new PangoImportException($"File {filePath} doesn't exist");
        if (Path.GetExtension(filePath) != PackageFileExtension) throw new PangoImportException($"Invalid file extension");
    }
}
