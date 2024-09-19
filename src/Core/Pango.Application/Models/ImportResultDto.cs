using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;

namespace Pango.Application.Models;

public record ImportResultDto
{
    public ImportResultDto(PangoPackageManifest manifest, List<IContentPackage> contentPackages)
    {
        Manifest = manifest;
        ContentPackages = contentPackages;
    }

    public PangoPackageManifest Manifest { get; init; }

    public List<IContentPackage> ContentPackages { get; init; }
}
