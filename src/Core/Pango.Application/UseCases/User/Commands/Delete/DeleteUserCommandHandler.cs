using ErrorOr;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.User.Commands.Delete;

public class DeleteUserCommandHandler
    : IRequestHandler<DeleteUserCommand, ErrorOr<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserDataRepository _userDataRepository;

    public DeleteUserCommandHandler(IUserRepository userRepository, IUserDataRepository userDataRepository)
    {
        _userRepository = userRepository;
        _userDataRepository = userDataRepository;
    }

    public async Task<ErrorOr<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            PangoUser? user = await _userRepository.FindAsync(request.UserName);

            if (user is null)
            {
                return Error.Failure("user_not_found", $"User \"{request.UserName}\" not found");
            }

            await _userRepository.DeleteAsync(user);
            await _userDataRepository.DeleteAllUserDataAsync(user.Id.ToString());

            return true;
        }
        catch (Exception ex)
        {
            return Error.Failure("cannot_delete_user", ex.Message);
        }
    }
}
