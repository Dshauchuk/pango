using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.User.Commands.Delete;

public class DeleteUserCommandHandler
    : IRequestHandler<DeleteUserCommand, ErrorOr<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUserStorageManager _userDataRepository;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(IUserRepository userRepository, IUserStorageManager userDataRepository, ILogger<DeleteUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _userDataRepository = userDataRepository;
        _logger = logger;
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
            await _userDataRepository.DeleteAllUserDataAsync(user.UserName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User {UserName} cannot be deleted: {Message}", request.UserName, ex.Message);
            return Error.Failure(ApplicationErrors.User.DeletionFailed, $"User {request.UserName} cannot be deleted: {ex.Message}");
        }
    }
}
