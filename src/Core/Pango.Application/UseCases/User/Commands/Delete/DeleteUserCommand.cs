using ErrorOr;
using MediatR;

namespace Pango.Application.UseCases.User.Commands.Delete;

public class DeleteUserCommand : IRequest<ErrorOr<bool>>
{
    public DeleteUserCommand(string userName)
    {
        UserName = userName;
    }

    public string UserName { get; set; }
}
