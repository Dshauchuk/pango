using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Domain.Entities;
using System.Text.Json;

namespace Pango.Application.UseCases.User.Commands.Register;

public class RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHashProvider passwordHashProvider, ILogger<RegisterUserCommandHandler> logger)
    : IRequestHandler<RegisterUserCommand, ErrorOr<PangoUserDto>>
{
    private const int MaxUserCount = 5;

    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHashProvider _passwordHashProvider = passwordHashProvider;
    private readonly ILogger<RegisterUserCommandHandler> _logger = logger;

    public async Task<ErrorOr<PangoUserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var existingUsers = await _userRepository.ListAsync();
            if (existingUsers.Count() >= MaxUserCount)
            {
                return Error.Validation(ApplicationErrors.User.TooManyUsers, $"Can't create more than {MaxUserCount} users");
            }

            var (passwordHash, salt) = await Task.Run(() =>
            {
                string hash = _passwordHashProvider.Hash(request.Password, out byte[] generatedSalt);
                return (hash, generatedSalt);
            });

            PangoUser user = new()
            {
                UserName = request.UserName,
                MasterPasswordHash = passwordHash,
                PasswordSalt = Convert.ToBase64String(salt),
            };

            await _userRepository.CreateAsync(user);

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var configPath = Path.Combine(appData, "Pango", "backup_config.json");
            BackupSettings backupSettings = new();

            if (File.Exists(configPath))
            {
                var json = await File.ReadAllTextAsync(configPath, cancellationToken);
                backupSettings = JsonSerializer.Deserialize<BackupSettings>(json) ?? new BackupSettings();
            }
            else
            {
                backupSettings.TargetFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pango", "Backup");
                backupSettings.IsEnabled = true;
            }

            backupSettings.Users ??= [];
            backupSettings.Users[user.UserName] = new UserBackupProfile
            {
                BackupPassword = Guid.NewGuid().ToString("N")[..12].ToUpper(),
                SourceDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Pango", "users", user.UserName)
            };

            await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(backupSettings), cancellationToken);

            return user.Adapt<PangoUserDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User {UserName} cannot be registered: {Message}", request.UserName, ex.Message);
            return Error.Failure(ApplicationErrors.User.RegistrationFailed, $"User {request.UserName} cannot be registered: {ex.Message}");
        }
    }
}
