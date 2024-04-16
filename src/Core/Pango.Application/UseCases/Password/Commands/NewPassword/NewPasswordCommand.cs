using ErrorOr;
using MediatR;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Commands.NewPassword;

public record NewPasswordCommand : IRequest<ErrorOr<PangoPasswordDto>>
{
    public NewPasswordCommand(string name, string login, string value, Dictionary<string, string>? properties = null)
    {
        Name = name;
        Login = login;
        Value = value;
        Properties = properties ?? new();
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
    /// Encrypted value of the password
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Password entry properties
    /// </summary>
    public Dictionary<string, string> Properties { get; set; }
}
