using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using System.IO.Packaging;
using Package = System.IO.Packaging.Package;

namespace Pango.Persistence.File;

public class PangoFileDataExporter(IContentEncoder contentEncoder, IAppDomainProvider appDomainProvider, ILogger<PangoFileDataExporter> logger) : IDataExporter
{
    private readonly IContentEncoder _contentEncoder = contentEncoder;
    private readonly IAppDomainProvider _appDomainProvider = appDomainProvider;
    private readonly ILogger _logger = logger;

    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private const string PackageFileExtension = ".pngx";
    private const int MaxRetries = 3;
    private const int DelayMilliseconds = 1000;

    public async Task<string> ExportAsync(PangoPackageManifest manifest, IEnumerable<IContentPackage> contentPackages, IExportOptions exportOptions)
    {
        await _semaphore.WaitAsync();

        string filePath = Path.Combine(_appDomainProvider.GetTempFolderPath(), $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}{PackageFileExtension}");

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Exporting {contentCount} data packages of {contentTypes} data type(s) into {filePath}",
                contentPackages.Count(), string.Join(",", contentPackages.Select(c => c.DataType)), filePath);
        }

        try
        {
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
                    using (Package package = Package.Open(fileStream, FileMode.Create))
                    {
                        var manifestPartUri = new Uri("/part0.pngdat", UriKind.Relative);
                        PackagePart manifestPart = package.CreatePart(manifestPartUri, "application/octet-stream", CompressionOption.Maximum);
                        byte[] encryptedManifestData = await _contentEncoder.EncryptAsync(manifest, exportOptions.EncodingOptions.Key, exportOptions.EncodingOptions.Salt);
                        using (Stream partStream = manifestPart.GetStream())
                        {
                            await partStream.WriteAsync(encryptedManifestData);
                            await partStream.FlushAsync();
                        }

                        int partIndex = 1;
                        foreach (IContentPackage data in contentPackages)
                        {
                            Uri partUri = new($"/part{partIndex}.pngdat", UriKind.Relative);
                            PackagePart part = package.CreatePart(partUri, "application/octet-stream", CompressionOption.Maximum);

                            byte[] encryptedData = await _contentEncoder.EncryptAsync(data, exportOptions.EncodingOptions.Key, exportOptions.EncodingOptions.Salt);

                            using (Stream partStream = part.GetStream())
                            {
                                await partStream.WriteAsync(encryptedData);
                            }
                            partIndex++;
                        }
                    }

                    await fileStream.DisposeAsync();

                    break;
                }
                catch (IOException e)
                {
                    _logger.LogError("An error occurred while exporting data to {filePath}: {message}. Attempt {attempt}/{maxRetries}", filePath, e.Message, attempt + 1, MaxRetries);
                    await Task.Delay(DelayMilliseconds);
                    if (attempt == MaxRetries - 1) throw;
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
