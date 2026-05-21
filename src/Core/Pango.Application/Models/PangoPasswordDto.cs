using Pango.Domain.Common;
using System.Diagnostics;

namespace Pango.Application.Models;

/// <summary>
/// Data Transfer Object for PangoPassword. Keeps data encrypted in RAM.
/// </summary>
public class PangoPasswordDto : DtoBase, IDisposable
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly RamProtectedString _protectedValue;

    public PangoPasswordDto()
    {
        _protectedValue = new RamProtectedString(string.Empty);
        Target = string.Empty;
        UserName = string.Empty;
        Name = string.Empty;
        Login = string.Empty;
        Star = false;
        Properties = [];
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
    /// Gets or sets the password value.
    /// </summary>
    public string Value
    {
        get => _protectedValue.GetDecryptedValue();
        set => _protectedValue.SetPlaintextValue(value);
    }

    /// <summary>
    /// A resource that the password is for
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Name of the password owner
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Indicates if this model is a dummy for keeping the catalog
    /// </summary>
    public bool IsCatalog { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public bool Star { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DateTimeOffset LastModifiedAt { get; set; }

    /// <summary>
    /// Path of the catalog, e.g. folder1/folder1_1
    /// </summary>
    public string CatalogPath { get; set; }

    /// <summary>
    /// Presents the path of the file where the password is located
    /// </summary>
    public string LocationPath { get; set; }

    public void Dispose()
    {
        _protectedValue.Dispose();
        GC.SuppressFinalize(this);
    }
}
