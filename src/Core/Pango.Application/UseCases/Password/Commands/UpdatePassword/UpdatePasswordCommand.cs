using ErrorOr;
using MediatR;
using Pango.Application.Models;
using Pango.Domain.Common;

namespace Pango.Application.UseCases.Password.Commands.UpdatePassword;

/// <summary>
/// Command to update an existing password entry.
/// </summary>
public record UpdatePasswordCommand : IRequest<ErrorOr<PangoPasswordDto>>
{
    private readonly RamProtectedString _protectedValue;

    public UpdatePasswordCommand(Guid id, string name, string login, string value, Dictionary<string, string>? properties = null)
    {
        PasswordId = id;
        Name = name;
        Login = login;
        _protectedValue = new RamProtectedString(value);
        Properties = properties ?? [];
        CatalogPath = string.Empty;
    }

    public Guid PasswordId { get; set; }

    /// <summary>
    /// A password title
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// User's login for the resource
    /// </summary>
    public string Login { get; set; }

    /// <summary>
    /// Decrypted password value. Exposed only for internal use; callers should use the encrypted form.
    /// </summary>
    public string Value => _protectedValue.GetDecryptedValue();

    /// <summary>
    /// Indicates if this model is a dummy for keeping the catalog
    /// </summary>
    public bool IsCatalogHolder { get; set; }

    /// <summary>
    /// Catalog path where the password entry is stored.
    /// </summary>
    public string CatalogPath { get; set; }

    /// <summary>
    /// Password entry properties
    /// </summary>
    public Dictionary<string, string> Properties { get; set; }
}
