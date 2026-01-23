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

    private static readonly SemaphoreSlim _semaphore = new (1, 1);
    private const int MaxRetries = 3;
    private const int DelayMiliseconds = 1000;
    private const string PackageFileExtension = ".pngx";

    public async Task<ImportResultDto> ImportAsync(string filePath, IImportOptions importOptions)
    {
        List<IContentPackage> importedPackages = [];
        PangoPackageManifest? manifest = null;

        await _semaphore.WaitAsync();

        string tempDecryptedPath = string.Empty;

        try
        {
            ThrowIfFileIsNotValid(filePath);

            // 1. Detect if the file is a standard Zip (Export) or Encrypted (Backup)
            bool isEncryptedBackup = false;
            try
            {
                using (ZipFile.OpenRead(filePath)) { }
            }
            catch (InvalidDataException)
            {
                isEncryptedBackup = true;
            }
            catch (Exception)
            {
                isEncryptedBackup = true;
            }

            string workingFilePath = filePath;

            // 2. Decrypt Backup Container (if applicable)
            if (isEncryptedBackup)
            {
                _logger.LogInformation("File appears encrypted. Attempting backup decryption...");
                tempDecryptedPath = Path.GetTempFileName();

                string backupPassword = importOptions.EncodingOptions.Key;

                try
                {
                    await DecryptBackupFileAsync(filePath, tempDecryptedPath, backupPassword);
                    workingFilePath = tempDecryptedPath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Backup decryption failed.");
                    throw new PangoDataDecryptionException(ApplicationErrors.Data.DecryptionError, "Invalid backup password.", ex);
                }
            }

            // Get User Keys
            EncodingOptions userOptions = await _userContextProvider.GetEncodingOptionsAsync();

            // 3. Process the Archive
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    _logger.LogDebug("Reading archive from {filePath}", workingFilePath);

                    using var fs = new FileStream(workingFilePath, FileMode.Open);

                    ZipArchive archive;
                    try
                    {
                        archive = new ZipArchive(fs, ZipArchiveMode.Read);
                    }
                    catch (InvalidDataException)
                    {
                        throw new PangoDataDecryptionException(ApplicationErrors.Data.DecryptionError, "Wrong password (archive is invalid).");
                    }

                    using (archive)
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.Name == "[Content_Types].xml" || entry.FullName.StartsWith("_rels") || string.IsNullOrEmpty(entry.Name))
                                continue;

                            using var entryStream = entry.Open();
                            using var ms = new MemoryStream();
                            await entryStream.CopyToAsync(ms);
                            byte[] encryptedData = ms.ToArray();

                            var decryptionOptions = isEncryptedBackup ? userOptions : importOptions.EncodingOptions;

                            if (manifest == null)
                            {
                                manifest = await TryDecrypt<PangoPackageManifest>(encryptedData, decryptionOptions);
                            }

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
                    }
                    break;
                }
                catch (PangoDataDecryptionException)
                {
                    throw;
                }
                catch (IOException e)
                {
                    _logger.LogError("Import attempt {attempt} failed: {message}", attempt + 1, e.Message);
                    await Task.Delay(DelayMiliseconds);
                    if (attempt == MaxRetries - 1) throw;
                }
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempDecryptedPath) && System.IO.File.Exists(tempDecryptedPath))
            {
                try { System.IO.File.Delete(tempDecryptedPath); } catch { }
            }
            _logger.LogDebug("Import completed");
            _semaphore.Release();
        }

        if (manifest is null && importedPackages.Count == 0)
        {
            throw new PangoDataDecryptionException(ApplicationErrors.Data.DecryptionError, "Wrong password or invalid file format.");
        }

        // 4. Construct Result
        if (manifest is null)
        {
            if (importedPackages.Count > 0)
            {
                manifest = new PangoPackageManifest(
                    _userContextProvider.GetUserName(),
                    DateTime.UtcNow.ToString("G"),
                    "Restored from Backup",
                    new Dictionary<Domain.Enums.ContentType, int> { { Domain.Enums.ContentType.Passwords, importedPackages.Count } }
                );
            }
            else
            {
                throw new PangoImportException("Import failed: No valid data found. Wrong password or wrong user account.");
            }
        }

        var finalManifest = manifest.Value;
        finalManifest.Contents ??= [];

        if (importedPackages.Count > 0 && (!finalManifest.Contents.ContainsKey(Domain.Enums.ContentType.Passwords) || finalManifest.Contents[Domain.Enums.ContentType.Passwords] == 0))
        {
            finalManifest.Contents[Domain.Enums.ContentType.Passwords] = importedPackages.Count;
        }

        return new ImportResultDto(finalManifest, importedPackages);
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
        if (await fsInput.ReadAsync(salt.AsMemory(0, 16)) < 16)
            throw new IOException("Invalid header");

        byte[] iv = new byte[16];
        if (await fsInput.ReadAsync(iv.AsMemory(0, 16)) < 16)
            throw new IOException("Invalid header");

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
        if (!System.IO.File.Exists(filePath))
            throw new PangoImportException($"File {filePath} doesn't exist");

        if (Path.GetExtension(filePath) != PackageFileExtension)
            throw new PangoImportException($"Invalid file extension");
    }
}
