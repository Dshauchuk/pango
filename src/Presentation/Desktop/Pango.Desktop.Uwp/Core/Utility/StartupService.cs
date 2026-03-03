using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel;
using Pango.Desktop.Uwp.Core.Utility.Contracts;

namespace Pango.Desktop.Uwp.Core.Utility;

/// <summary>
/// Utility class for managing the application's "Startup Task" entry defined in the package manifest.
/// </summary>
/// <param name="logger">Optional logger for diagnostics.</param>
public class StartupService(ILogger<StartupService>? logger = null) : IStartupService
{
    private const string TaskId = "PangoStartupTask";

    private readonly ILogger<StartupService>? _logger = logger;

    /// <summary>
    /// Determines whether the application is currently registered to launch on OS startup.
    /// </summary>
    public async Task<bool> IsStartupEnabledAsync()
    {
        try
        {
            var startupTask = await StartupTask.GetAsync(TaskId);
            return startupTask.State == StartupTaskState.Enabled;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to query startup-task state: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Attempts to enable the application to start with the OS.
    /// </summary>
    public async Task<bool> EnableStartupAsync()
    {
        try
        {
            var startupTask = await StartupTask.GetAsync(TaskId);

            if (startupTask.State == StartupTaskState.DisabledByPolicy ||
                startupTask.State == StartupTaskState.DisabledByUser)
            {
                _logger?.LogWarning("Startup-task is disabled by policy/user and cannot be enabled programmatically.");
                return false;
            }

            var state = await startupTask.RequestEnableAsync();
            return state == StartupTaskState.Enabled;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to enable startup-task: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Disables the application from starting with the OS.
    /// </summary>
    public async Task DisableStartupAsync()
    {
        try
        {
            var startupTask = await StartupTask.GetAsync(TaskId);
            startupTask.Disable();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Failed to disable startup-task: {Message}", ex.Message);
        }
    }
}
