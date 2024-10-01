using Pango.Application.Common;

namespace Pango.Application.UseCases.Data.Commands.Import;

public class ImportResult
{
    public ImportResult(PangoPackageManifest manifest)
    {
        Manifest = manifest;
    }

    public PangoPackageManifest Manifest { get; init; }
}
