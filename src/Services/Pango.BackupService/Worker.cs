using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using Pango.Infrastructure.Services;
using System.Text.Json;

namespace Pango.BackupService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IBackupManager _backupManager;
    private readonly string _configPath;

    // Path where UWP app stores user data (LocalState/users)
    private readonly string _appDataRoot;

    public Worker(ILogger<Worker> logger, IBackupManager backupManager)
    {
        _logger = logger;
        _backupManager = backupManager;

        // Path to the shared config file in ProgramData
        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        _configPath = Path.Combine(commonAppData, "Pango", "backup_config.json");

        // Construct path to the UWP LocalState/users folder
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _appDataRoot = Path.Combine(userProfile, "AppData", "Local", "Packages", "Pango_p2sx85d", "LocalState", "users");
    }

    // Main execution loop of the Windows Service
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = LoadSettings();

            if (settings != null && settings.IsEnabled && settings.Users != null)
            {
                foreach (var userEntry in settings.Users)
                {
                    string userId = userEntry.Key;
                    UserBackupProfile profile = userEntry.Value;

                    if (string.IsNullOrEmpty(profile.SourceDataPath) || !Directory.Exists(profile.SourceDataPath))
                    {
                        _logger.LogWarning("Source path for user {User} is invalid or empty. Run the App to configure.", userId);
                        continue;
                    }

                    if (_backupManager is BackupManager manager)
                    {
                        await manager.PerformBackupForUserAsync(
                            userId,
                            profile.SourceDataPath,
                            settings.TargetFolderPath,
                            profile.BackupPassword);
                    }
                }

                int interval = settings.IntervalMinutes > 0 ? settings.IntervalMinutes : 60;
                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
            else
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    // Reads the JSON configuration file from disk
    private BackupSettings? LoadSettings()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                string json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<BackupSettings>(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load backup configuration from {Path}", _configPath);
        }
        return null;
    }
}