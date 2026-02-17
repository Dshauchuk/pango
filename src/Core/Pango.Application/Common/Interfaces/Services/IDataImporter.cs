using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;

namespace Pango.Application.Common.Interfaces.Services;

public interface IDataImporter
{
    public Task<ImportResultDto> ImportAsync(string filePath, IImportOptions importOptions);

    Task<PangoPackageManifest> ReadManifestAsync(string filePath, IImportOptions importOptions);

    Task<List<IContentPackage>> ExtractContentAsync(string filePath, IImportOptions importOptions);
}
