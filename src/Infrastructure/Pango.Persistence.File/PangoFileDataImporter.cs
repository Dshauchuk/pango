using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using System.IO.Packaging;

namespace Pango.Persistence.File;

public class PangoFileDataImporter : IDataImporter
{
    private readonly IContentEncoder _contentEncoder;
    private readonly IAppDomainProvider _appDomainProvider;
    private readonly ILogger _logger;

    private static readonly SemaphoreSlim _semaphore = new (1, 1);
    private const int MaxRetries = 3;
    private const int DelayMiliseconds = 1000;
    private const string PackageFileExtension = ".pngx";

    public PangoFileDataImporter(IContentEncoder contentEncoder, IAppDomainProvider appDomainProvider, ILogger<PangoFileDataImporter> logger)
    {
        _contentEncoder = contentEncoder;
        _appDomainProvider = appDomainProvider;
        _logger = logger;
    }

    public async Task<List<IContentPackage>> ImportAsync(string filePath, IImportOptions importOptions)
    {
        List<IContentPackage> importedPackages = [];

        await _semaphore.WaitAsync();

        ThrowIfFileIsNotValid(filePath);

        try
        {
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    _logger.LogDebug("Importing data from {filePath}", filePath);

                    // Open the package
                    using Package package = Package.Open(filePath, FileMode.Open, FileAccess.Read);
                    foreach (PackagePart part in package.GetParts())
                    {
                        // Read the encrypted data from the package part
                        using Stream partStream = part.GetStream();
                        byte[] encryptedData = new byte[partStream.Length];
                        partStream.Read(encryptedData, 0, encryptedData.Length);

                        // Decrypt the data
                        IContentPackage? data = await _contentEncoder.DecryptAsync<ContentPackage>(encryptedData, importOptions.EncodingOptions.Key, importOptions.EncodingOptions.Salt);

                        if (data is null)
                        {
                            _logger.LogWarning("Got an empty package while importing data from {filePath}", filePath);
                        }
                        else
                        {
                            importedPackages.Add(data);
                        }
                    }

                    // If successful, break out of the loop
                    break;
                }
                catch(IOException e)
                {
                    _logger.LogError("An error occurred while importing data from {filePath}: {message}. Attempt {attempt}/{maxRetries}", filePath, e.Message, attempt + 1, MaxRetries);

                    await Task.Delay(DelayMiliseconds);

                    if(attempt == MaxRetries - 1)
                    {
                        throw;
                    }
                }
            }
        }
        finally
        {
            _logger.LogDebug("Import completed");
            _semaphore.Release();
        }

        return importedPackages;
    }

    /// <summary>
    /// Throws <see cref="PangoImportException"/> if file <paramref name="filePath"/> is not valid
    /// </summary>
    /// <param name="filePath"></param>
    /// <exception cref="PangoImportException"></exception>
    private void ThrowIfFileIsNotValid(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            throw new PangoImportException($"File {filePath} doesn't exist and cannot be imported");
        }

        if(Path.GetExtension(filePath) != PackageFileExtension)
        {
            throw new PangoImportException($"Invalid file {filePath}: unsupported file extension");
        }
    }
}
