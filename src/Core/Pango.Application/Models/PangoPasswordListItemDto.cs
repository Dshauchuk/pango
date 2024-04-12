namespace Pango.Application.Models;

public class PangoPasswordListItemDto : DtoBase
{
    public PangoPasswordListItemDto(IEnumerable<PangoPasswordListItemDto> children)
    {
        Children = children;
    }

    public PangoPasswordListItemDto()
    {
        Children = new List<PangoPasswordListItemDto>();
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
    public IEnumerable<PangoPasswordListItemDto> Children { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset LastModifiedAt { get; set; }
}
