using ErrorOr;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.User.Commands.SignIn;

public class SignInCommandHandler : IRequestHandler<SignInCommand, ErrorOr<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashProvider _passwordHashProvider;

    public SignInCommandHandler(IUserRepository userRepository, IPasswordHashProvider passwordHashProvider)
    {
        _userRepository = userRepository;
        _passwordHashProvider = passwordHashProvider;
    }

    public async Task<ErrorOr<bool>> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        try
        {
            PangoUser? user = await _userRepository.FindAsync(request.UserName);

            if (user is null)
                return false;

            return _passwordHashProvider.VerifyPassword(request.Password, user.MasterPasswordHash, new byte[0]);
        }
        catch(Exception ex)
        {
            return Error.Failure("authentication_error", ex.Message);
        }
    }
}
