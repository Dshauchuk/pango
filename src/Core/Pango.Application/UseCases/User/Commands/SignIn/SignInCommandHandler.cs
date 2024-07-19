using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.User.Commands.SignIn;

public class SignInCommandHandler : IRequestHandler<SignInCommand, ErrorOr<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashProvider _passwordHashProvider;
    private readonly ILogger<SignInCommandHandler> _logger;

    public SignInCommandHandler(IUserRepository userRepository, IPasswordHashProvider passwordHashProvider, ILogger<SignInCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHashProvider = passwordHashProvider;
        _logger = logger;
    }

    public async Task<ErrorOr<bool>> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        try
        {
            PangoUser? user = await _userRepository.FindAsync(request.UserName);

            if (user is null)
                return false;

            return _passwordHashProvider.VerifyPassword(request.Password, user.MasterPasswordHash, Convert.FromBase64String(user.PasswordSalt));
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Login failed for user {UserName}: {Message}", request.UserName, ex.Message);
            return Error.Failure(ApplicationErrors.User.LoginFailed, ex.Message);
        }
    }
}
