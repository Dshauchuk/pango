﻿namespace Pango.Application.Models;

public class PangoPasswordListItemDto : DtoBase
{
    /// <summary>
    /// A password title
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset LastModifiedAt { get; set; }
}
