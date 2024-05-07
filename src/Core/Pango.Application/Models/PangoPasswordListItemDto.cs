namespace Pango.Application.Models;

public class PangoPasswordListItemDto : DtoBase
{
    public PangoPasswordListItemDto(IEnumerable<PangoPasswordListItemDto> children)
    {
        Children = children?.ToList() ?? new List<PangoPasswordListItemDto>();
        CatalogPath = string.Empty;
    }

    public PangoPasswordListItemDto()
    {
        Children = new List<PangoPasswordListItemDto>();
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
    public List<PangoPasswordListItemDto> Children { get; set; }

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
