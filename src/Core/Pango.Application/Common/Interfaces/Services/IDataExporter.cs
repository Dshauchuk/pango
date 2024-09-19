using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Enums;

namespace Pango.Application.Common.Interfaces.Services;

public record struct PangoPackageManifest
{
    public PangoPackageManifest(string createdByUser, string createdAt, string description, Dictionary<ContentType, int> contents)
    {
        CreatedByUser = createdByUser;
        CreatedAt = createdAt;
        Description = description;
        Contents = contents;
    }

    public string CreatedByUser { get; }
    public string CreatedAt { get; }
    public string Description { get; set; }
    public Dictionary<ContentType, int> Contents { get; }
}

public interface IDataExporter
{
    Task<string> ExportAsync(PangoPackageManifest manifest, IEnumerable<IContentPackage> contentPackages, IExportOptions exportOptions);
}
