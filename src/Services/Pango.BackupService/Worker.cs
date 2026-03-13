using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using System.Text.Json;

namespace Pango.BackupService;

/// <summary>
/// Background worker service that handles the automated data backup cycle.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IBackupManager _backupManager;
    private readonly string _configPath;

    /// <summary>
    /// Initializes the Worker with required services and paths.
    /// </summary>
    public Worker(ILogger<Worker> logger, IBackupManager backupManager)
    {
        _logger = logger;
        _backupManager = backupManager;

        // Path to config in ProgramData (accessible by Service)
        string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        _configPath = Path.Combine(commonAppData, "Pango", "backup_config.json");
    }

    /// <summary>
    /// Main execution loop running continuously in the background until cancellation is requested.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
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
                            _logger.LogWarning("Invalid source path for user {User}. Skipping.", userId);
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
                    // Retry shortly if settings are missing or disabled
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                // Expected exception when the service is stopped/killed
                _logger.LogInformation("Backup background task was canceled.");
                break;
            }
            catch (Exception ex)
            {
                // Prevent background service from crashing completely on unexpected error
                _logger.LogError(ex, "An unexpected error occurred during the backup cycle.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Loads and deserializes backup configuration settings from the local file system.
    /// </summary>
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
            _logger.LogError(ex, "Config load failed at path: {Path}", _configPath);
        }

        return null;
    }
}