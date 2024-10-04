using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Enums;

namespace Pango.Application.Common;

public class ContentPackage : IContentPackage
{
    public ContentPackage()
    {
        Id = Guid.NewGuid();
    }

    public ContentPackage(string user, ContentType contentType, string dataType, int itemsCount, object data, DateTimeOffset lastModifiedAt)
        : this()
    {
        Owner = user;
        ContentType = contentType;
        DataType = dataType;
        ContentItemsCount = itemsCount;
        Data = data;
        LastModifiedAt = lastModifiedAt;
    }

    /// <summary>
    /// Package id
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// ID of the user who owns the file
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Type of the content stored in the file
    /// </summary>
    public ContentType ContentType { get; set; }

    /// <summary>
    /// Type of the data stored in the file: e.g. IEnumerable<Password> etc.
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// Presents a number of items of <see cref="ContentType"/> stored in the file
    /// </summary>
    public int ContentItemsCount { get; set; }

    /// <summary>
    /// File content
    /// </summary>
    public object? Data { get; set; }

    public DateTimeOffset LastModifiedAt { get; set; }
}
