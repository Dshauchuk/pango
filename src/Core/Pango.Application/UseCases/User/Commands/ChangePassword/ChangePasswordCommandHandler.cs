using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.User.Commands.ChangePassword;

public class ChangePasswordCommandHandler
    : IRequestHandler<ChangePasswordCommand, ErrorOr<bool>>
{
    private readonly IUserStorageManager _userStorageManager;
    private readonly IPasswordHashProvider _passwordHashProvider;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserStorageManager userStorageManager,
        IPasswordHashProvider passwordHashProvider,
        IUserRepository userRepository,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _logger = logger;
        _userStorageManager = userStorageManager;
        _passwordHashProvider = passwordHashProvider;
        _userRepository = userRepository;
    }

    async Task<ErrorOr<bool>> IRequestHandler<ChangePasswordCommand, ErrorOr<bool>>.Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Command ChangePasswordCommandHandler triggered");

        try
        {
            _logger.LogDebug("Hashing the new password...");
            string passwordHash = _passwordHashProvider.Hash(request.Password, out var salt);
            EncodingOptions encoding = new(passwordHash, Convert.ToBase64String(salt));
            _logger.LogDebug("Hashing completed");
            
            _logger.LogDebug("Encrypting data with new password...");
            await _userStorageManager.EncryptDataWithAsync(request.UserId, encoding);
            _logger.LogDebug("New password applied");

            _logger.LogDebug("Updating user's credentials...");
            PangoUser? currentUser = await _userRepository.FindAsync(request.UserId);
            if ((currentUser is null))
            {
                throw new PangoException(ApplicationErrors.User.NotFound, $"User \"{request.UserId}\" not found");
            }
            await _userRepository.DeleteAsync(currentUser);

            currentUser.MasterPasswordHash = passwordHash;
            currentUser.PasswordSalt = Convert.ToBase64String(salt);
            await _userRepository.CreateAsync(currentUser);
            _logger.LogDebug("User's credentials updated");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChangePasswordCommand failed: {Message}", ex.Message);
            return false;
        }
    }
}
