﻿using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;

namespace Pango.Application.UseCases.Data.Commands.Import;

public class ImportResult
{
    public ImportResult(PangoPackageManifest manifest, List<IContentPackage> contentPackages)
    {
        ContentPackages = contentPackages;
        Manifest = manifest;
    }

    public PangoPackageManifest Manifest { get; init; }

    public List<IContentPackage> ContentPackages { get; init; }
}
