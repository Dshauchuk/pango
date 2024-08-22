﻿using Microsoft.Extensions.Logging;
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

    public PangoFileDataImporter(IContentEncoder contentEncoder, IAppDomainProvider appDomainProvider, ILogger<PangoFileDataImporter> logger)
    {
        _contentEncoder = contentEncoder;
        _appDomainProvider = appDomainProvider;
        _logger = logger;
    }

    public async Task<List<IContentPackage>> ImportAsync(string filePath, IImportOptions importOptions)
    {
        // todo: lock the file

        if (!System.IO.File.Exists(filePath))
        {
            throw new PangoImportException($"File {filePath} doesn't exist and cannot be imported");
        }

        _logger.LogDebug("Importing data from {filePath}", filePath);

        List<IContentPackage> importedPackages = [];

        // Open the package
        using (Package package = Package.Open(filePath, FileMode.Open, FileAccess.Read))
        {
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
        }

        _logger.LogDebug("Import completed");

        return importedPackages;
    }
}
