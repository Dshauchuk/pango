using ErrorOr;
using MediatR;
using Pango.Application.Models;
using Pango.Domain.Common;

namespace Pango.Application.UseCases.Password.Commands.NewPassword;

/// <summary>
/// Command to create a new password entry.
/// </summary>
public record NewPasswordCommand : IRequest<ErrorOr<PangoPasswordDto>>
{
    private readonly RamProtectedString _protectedValue;

    public NewPasswordCommand(string name, string login, string value, Dictionary<string, string>? properties = null)
    {
        Name = name;
        Login = login;
        _protectedValue = new RamProtectedString(value);
        CatalogPath = string.Empty;
        Properties = properties ?? [];
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
    /// Indicates if this model is a dummy for keeping the catalog
    /// </summary>
    public bool IsCatalogHolder { get; set; }

    /// <summary>
    /// Catalog path where the password entry is stored.
    /// </summary>
    public string CatalogPath { get; set; }

    /// <summary>
    /// Decrypted password value. Exposed only for internal use; callers should use the encrypted form.
    /// </summary>
    public string Value => _protectedValue.GetDecryptedValue();
    /// <summary>
    /// Password entry properties
    /// </summary>
    public Dictionary<string, string> Properties { get; set; }
    /// <summary>
    /// Is the favorite password
    /// </summary>
    public bool Star { get; set; }
}
