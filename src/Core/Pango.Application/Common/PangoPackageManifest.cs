using Pango.Domain.Enums;

namespace Pango.Application.Common;

public record struct PangoPackageManifest
{
    public PangoPackageManifest(string createdByUser, string createdAt, string description, Dictionary<ContentType, int> contents)
    {
        CreatedByUser = createdByUser;
        CreatedAt = createdAt;
        Description = description;
        Contents = contents;
    }

    public string CreatedByUser { get; set; }
    public string CreatedAt { get; set; }
    public string Description { get; set; }
    public Dictionary<ContentType, int> Contents { get; set; }
}
