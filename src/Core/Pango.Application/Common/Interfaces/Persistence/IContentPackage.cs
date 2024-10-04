using Pango.Domain.Enums;

namespace Pango.Application.Common.Interfaces.Persistence;

public interface IContentPackage : IHaveEncodedData
{
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
    /// Presents a number of items of <see cref="ContentType"/> stored in the file
    /// </summary>
    public int ContentItemsCount { get; set; }

    public DateTimeOffset LastModifiedAt { get; set; }
}
