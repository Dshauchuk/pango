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

    private const string PackageFileExtension = ".pngx";

    public PangoFileDataExporter(IContentEncoder contentEncoder, IAppDomainProvider appDomainProvider, ILogger<PangoFileDataExporter> logger)
    {
        _contentEncoder = contentEncoder;
        _appDomainProvider = appDomainProvider;
        _logger = logger;
    }

    public async Task<string> ExportAsync(IEnumerable<IContentPackage> contentPackages, IExportOptions exportOptions)
    {
        // todo: retry


        string filePath = Path.Combine(_appDomainProvider.GetTempFolderPath(), $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}{PackageFileExtension}");

        _logger.LogDebug(
            "Exporting {contentCound} data packages of {contentTypes} data type(s) into {filePath}",
            contentPackages.Count(), string.Join(",", contentPackages.Select(c => c.DataType)), filePath);

        try
        {
            using (Package package = Package.Open(filePath, FileMode.Create))
            {
                int partIndex = 0;
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
            }

            _logger.LogDebug("Export completed");

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while exporting data into file {filePath}", filePath);
            throw new PangoExportException("An error occurred while exporting data", ex);
        }
    }
}
