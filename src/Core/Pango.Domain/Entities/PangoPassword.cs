﻿using Pango.Domain.Common;

namespace Pango.Domain.Entities;

/// <summary>
/// A password persisted in pango
/// </summary>
public class PangoPassword : BaseAuditableEntity, ICataloguable
{
	public PangoPassword()
	{
		Id = Guid.NewGuid();
        Value = string.Empty;
        Target = string.Empty;
        UserName = string.Empty;
        Name = string.Empty;
        Login = string.Empty;
        Properties = new();
        CatalogPath = string.Empty;
        LocationPath = string.Empty;
    }

	/// <summary>
	/// A password title
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// User's login for the resource
	/// </summary>
	public string Login { get; set; }

	/// <summary>
	/// Password entry properties
	/// </summary>
	public Dictionary<string, string> Properties { get; set; }

	/// <summary>
	/// Encrypted value of the password
	/// </summary>
	public string Value { get; set; }

	/// <summary>
	/// A resource that the password is for
	/// </summary>
	public string Target { get; set; }

    /// <summary>
    /// Name of the password owner
    /// </summary>
    public string UserName { get; set; }
	
	/// <summary>
	/// Path of the catalog, e.g. folder1/folder1_1
	/// </summary>
    public string CatalogPath { get; set; }

	/// <summary>
	/// Indicated if the entity is intended for being a catalog owner
	/// </summary>
	public bool IsCatalog { get; set; }
	
	/// <summary>
	/// Presents the path of the file where the password is located
	/// </summary>
	public string LocationPath { get; set; }
}
