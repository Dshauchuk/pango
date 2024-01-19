using ErrorOr;
using MediatR;

namespace Pango.Application.UseCases.User.Commands.SignIn;

public record SignInCommand : IRequest<ErrorOr<bool>>
{
    public SignInCommand(string userName, string password)
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
