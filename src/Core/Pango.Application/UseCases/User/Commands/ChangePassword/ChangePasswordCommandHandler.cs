using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.User.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    IUserStorageManager userStorageManager,
    IPasswordHashProvider passwordHashProvider,
    IUserRepository userRepository,
    ILogger<ChangePasswordCommandHandler> logger)
        : IRequestHandler<ChangePasswordCommand, ErrorOr<bool>>
{
    private readonly IUserStorageManager _userStorageManager = userStorageManager;
    private readonly IPasswordHashProvider _passwordHashProvider = passwordHashProvider;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger<ChangePasswordCommandHandler> _logger = logger;

    async Task<ErrorOr<bool>> IRequestHandler<ChangePasswordCommand, ErrorOr<bool>>.Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Command ChangePasswordCommandHandler triggered");

        try
        {
            _logger.LogDebug("Hashing the new password...");
            var (passwordHash, salt) = await Task.Run(() =>
            {
                string hash = _passwordHashProvider.Hash(request.Password, out byte[] generatedSalt);
                return (hash, generatedSalt);
            });
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
