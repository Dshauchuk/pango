using ErrorOr;
using MediatR;
using Pango.Application.Models;

public record RegisterUserCommand : IRequest<ErrorOr<PangoUserDto>>
{
    public RegisterUserCommand(string userName, string password)
    {
        UserName = userName;
        Password = password;
    }

    /// <summary>
    /// A user name
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// User's master password (original)
    /// </summary>
    public string Password { get; set; }
}