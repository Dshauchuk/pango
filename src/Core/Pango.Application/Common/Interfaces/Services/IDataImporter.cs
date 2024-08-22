using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Application.Common.Interfaces.Services;

public interface IDataImporter
{
    public Task<List<IContentPackage>> ImportAsync(string filePath, IImportOptions importOptions);
}
