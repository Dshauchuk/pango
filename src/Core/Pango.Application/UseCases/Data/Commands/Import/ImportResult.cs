using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Application.UseCases.Data.Commands.Import;

public class ImportResult
{
    public ImportResult(List<IContentPackage> contentPackages)
    {
        ContentPackages = contentPackages;
    }

    public List<IContentPackage> ContentPackages { get; }
}
