using ErrorOr;
using MediatR;

namespace Pango.Application.UseCases.User.Commands.ChangePassword;

public class ChangePasswordCommand : IRequest<ErrorOr<bool>>
{
    public ChangePasswordCommand(string userId, string password, string salt)
    {
        Password = password;
        UserId = userId;
        Salt = salt;
    }

    public string Password { get; }
    public string Salt { get; }
    public string UserId { get; }
}
