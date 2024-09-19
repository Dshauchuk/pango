using Microsoft.Extensions.Logging;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using System.IO.Packaging;
using Package = System.IO.Packaging.Package;

namespace Pango.Persistence.File;

public class PangoFileDataExporter : IDataExporter
{
    private readonly IContentEncoder _contentEncoder;
    private readonly IAppDomainProvider _appDomainProvider;
    private readonly ILogger _logger;

    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private const string PackageFileExtension = ".pngx";
    private const int MaxRetries = 3;
    private const int DelayMiliseconds = 1000;

    public PangoFileDataExporter(IContentEncoder contentEncoder, IAppDomainProvider appDomainProvider, ILogger<PangoFileDataExporter> logger)
    {
        _contentEncoder = contentEncoder;
        _appDomainProvider = appDomainProvider;
        _logger = logger;
    }

    public async Task<string> ExportAsync(PangoPackageManifest manifest, IEnumerable<IContentPackage> contentPackages, IExportOptions exportOptions)
    {
        await _semaphore.WaitAsync();

        string filePath = Path.Combine(_appDomainProvider.GetTempFolderPath(), $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}{PackageFileExtension}");

        _logger.LogDebug(
            "Exporting {contentCound} data packages of {contentTypes} data type(s) into {filePath}",
            contentPackages.Count(), string.Join(",", contentPackages.Select(c => c.DataType)), filePath);

        try
        {
            for(int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    using Package package = Package.Open(filePath, FileMode.Create);

                    var manifestPartUri = new Uri($"/part0.pngdat", UriKind.Relative);
                    PackagePart manifestPart = package.CreatePart(manifestPartUri, "application/octet-stream", CompressionOption.Maximum);
                    byte[] encryptedManifestData = await _contentEncoder.EncryptAsync(manifest, exportOptions.EncodingOptions.Key, exportOptions.EncodingOptions.Salt);
                    using (Stream partStream = manifestPart.GetStream())
                    {
                        partStream.Write(encryptedManifestData, 0, encryptedManifestData.Length);
                    }

                    int partIndex = 1;
                    foreach (IContentPackage data in contentPackages)
                    {
                        string partUriString = $"/part{partIndex}.pngdat";
                        Uri partUri = new(partUriString, UriKind.Relative);
                        PackagePart part = package.CreatePart(partUri, "application/octet-stream", CompressionOption.Maximum);

                        // Encrypt the object data
                        byte[] encryptedData = await _contentEncoder.EncryptAsync(data, exportOptions.EncodingOptions.Key, exportOptions.EncodingOptions.Salt);

                        // Write the encrypted data to the package part
                        using (Stream partStream = part.GetStream())
                        {
                            partStream.Write(encryptedData, 0, encryptedData.Length);
                        }

                        partIndex++;
                    }

                    // If successful, break out of the loop
                    break;
                }
                catch (IOException e)
                {
                    _logger.LogError("An error occurred while exporting date from {filePath}: {message}. Attempt {attempt}/{maxRetries}", filePath, e.Message, attempt + 1, MaxRetries);

                    await Task.Delay(DelayMiliseconds);

                    if (attempt == MaxRetries - 1)
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while exporting data into file {filePath}", filePath);
                    throw new PangoExportException("An error occurred while exporting data", ex);
                }
            }
        }
        finally
        {
            _logger.LogDebug("Export completed");
            _semaphore.Release();
        }


        return filePath;
    }
}
