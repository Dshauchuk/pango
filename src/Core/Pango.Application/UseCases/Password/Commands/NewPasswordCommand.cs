using ErrorOr;
using MediatR;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Commands;

public record NewPasswordCommand : IRequest<ErrorOr<PasswordDto>>
{
    public NewPasswordCommand()
    {
        Name = string.Empty;
        Login = string.Empty;
        Value = string.Empty;
        Properties = new();
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
    /// Encrypted value of the password
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Password entry properties
    /// </summary>
    public Dictionary<string, string> Properties { get; set; }
}
