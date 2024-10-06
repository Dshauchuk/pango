using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.User.Commands.Register;

public class RegisterUserCommandHandler
: IRequestHandler<RegisterUserCommand, ErrorOr<PangoUserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashProvider _passwordHashProvider;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHashProvider passwordHashProvider, ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHashProvider = passwordHashProvider;
        _logger = logger;
    }

    public async Task<ErrorOr<PangoUserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            string passwordHash = _passwordHashProvider.Hash(request.Password, out var salt);

            PangoUser user = new()
            {
                UserName = request.UserName,
                MasterPasswordHash = passwordHash,
                PasswordSalt = Convert.ToBase64String(salt),
            };

            await _userRepository.CreateAsync(user);

            return user.Adapt<PangoUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User {UserName} cannot be registered: {Message}", request.UserName, ex.Message);
            return Error.Failure(ApplicationErrors.User.RegistrationFailed, $"User {request.UserName} cannot be registered: {ex.Message}");
        }
    }
}
