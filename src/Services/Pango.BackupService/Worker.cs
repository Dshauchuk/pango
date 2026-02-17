using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using System.Text.Json;

namespace Pango.BackupService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IBackupManager _backupManager;
    private readonly string _configPath;

    // Constructor initializes logger, manager and paths
    public Worker(ILogger<Worker> logger, IBackupManager backupManager)
    {
        _logger = logger;
        _backupManager = backupManager;

        // Path to config in ProgramData (accessible by Service)
        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        _configPath = Path.Combine(commonAppData, "Pango", "backup_config.json");
    }

    // Main service loop executing background tasks
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

                    // Skip if source path is invalid
                    if (string.IsNullOrEmpty(profile.SourceDataPath) || !Directory.Exists(profile.SourceDataPath))
                    {
                        _logger.LogWarning("Invalid source path for user {User}", userId);
                        continue;
                    }

                    // Execute backup logic
                    await _backupManager.PerformBackupForUserAsync(
                        userId,
                        profile.SourceDataPath,
                        settings.TargetFolderPath,
                        profile.BackupPassword,
                        settings.RetentionDays > 0 ? settings.RetentionDays : 7);
                }

                // Wait for the next cycle
                int interval = settings.IntervalMinutes > 0 ? settings.IntervalMinutes : 60;
                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
            else
            {
                // Retry shortly if no settings found
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    // Helper to read configuration from disk
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
            _logger.LogError(ex, "Config load failed: {Path}", _configPath);
        }
        return null;
    }
}