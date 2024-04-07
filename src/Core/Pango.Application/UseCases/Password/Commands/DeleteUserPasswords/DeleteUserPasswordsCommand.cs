using ErrorOr;
using MediatR;

namespace Pango.Application.UseCases.Password.Commands.DeleteUserPasswords;

public record DeleteUserPasswordsCommand : IRequest<ErrorOr<bool>>
{
    public DeleteUserPasswordsCommand(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; set; }
}
