using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Application.Common.Interfaces.Services;

public interface IDataExporter
{
    Task<string> ExportAsync(PangoPackageManifest manifest, IEnumerable<IContentPackage> contentPackages, IExportOptions exportOptions);
}
