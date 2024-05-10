using ErrorOr;
using MediatR;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Commands.UpdatePassword;

public record UpdatePasswordCommand : IRequest<ErrorOr<PangoPasswordDto>>
{
    public UpdatePasswordCommand(Guid id, string name, string login, string value, Dictionary<string, string>? properties = null)
    {
        PasswordId = id;
        Name = name;
        Login = login;
        Value = value;
        Properties = properties ?? new();
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
    /// Encrypted value of the password
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string CatalogPath { get; set; }

    /// <summary>
    /// Password entry properties
    /// </summary>
    public Dictionary<string, string> Properties { get; set; }
}
