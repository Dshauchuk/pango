namespace Pango.Application.Models;

public class PangoPasswordListItemDto : DtoBase
{
    public PangoPasswordListItemDto(IEnumerable<PangoPasswordListItemDto> children)
    {
        CatalogPath = string.Empty;
    }

    public PangoPasswordListItemDto()
    {
        CatalogPath = string.Empty;
    }

    /// <summary>
    /// A password title
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the entry is a catalog, not password
    /// </summary>
    public bool IsCatalog { get; set; } 

    /// <summary>
    /// 
    /// </summary>
    public string CatalogPath { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset LastModifiedAt { get; set; }
}
