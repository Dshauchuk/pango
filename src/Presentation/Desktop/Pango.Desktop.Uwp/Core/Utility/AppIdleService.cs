using Microsoft.UI.Xaml;
using Pango.Desktop.Uwp.Core.Utility.Contracts;
using Pango.Desktop.Uwp.Models;
using Serilog;
using System.Runtime.InteropServices;

namespace Pango.Desktop.Uwp.Core.Utility;

/// <summary>
/// Service for tracking application idle time and executing actions when idle threshold is reached.
/// </summary>
public class AppIdleService : IAppIdleService
{
    private readonly Dictionary<Guid, TimerAction> _timerActions = [];

    #region Win32 Interop

#pragma warning disable SYSLIB1054 // DllImport is acceptable for simple P/Invoke scenarios
    /// <summary>
    /// Retrieves the time of the last input event (keyboard or mouse).
    /// </summary>
    /// <param name="plii">Reference to LASTINPUTINFO structure to receive the information.</param>
    /// <returns>True if successful; false otherwise.</returns>
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
#pragma warning restore SYSLIB1054

    /// <summary>
    /// Structure containing information about the last input event.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        /// <summary>
        /// Size of this structure, in bytes.
        /// </summary>
        public uint cbSize;

        /// <summary>
        /// Time of the last input event, in ticks since system startup.
        /// </summary>
        public uint dwTime;
    }

    #endregion

    /// <summary>
    /// Starts monitoring for application idle time and executes the specified action when threshold is reached.
    /// </summary>
    /// <param name="timeOfIdle">The idle time threshold after which the action should be triggered.</param>
    /// <param name="onIdle">The action to execute when idle threshold is reached.</param>
    /// <returns>Unique identifier for the registered idle timer, used to stop monitoring later.</returns>
    public Guid StartAppIdle(TimeSpan timeOfIdle, Action onIdle)
    {
        Log.Logger?.Debug("StartAppIdle: registering idle monitor with threshold {Threshold}", timeOfIdle);

        void TimerTickHandler(object? sender, object e)
        {
            var lii = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };

            if (GetLastInputInfo(ref lii))
            {
                uint elapsedTicks = unchecked((uint)Environment.TickCount - lii.dwTime);
                var idleTime = TimeSpan.FromMilliseconds(elapsedTicks);

                Log.Logger?.Debug("Idle check: {IdleTime} elapsed (threshold: {Threshold})", idleTime, timeOfIdle);

                if (idleTime >= timeOfIdle)
                {
                    Log.Logger?.Information("Idle threshold reached: executing onIdle action");
                    onIdle();
                }
            }
            else
            {
                Log.Logger?.Warning("GetLastInputInfo failed: unable to retrieve last input time");
            }
        }

        var idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        idleTimer.Tick += TimerTickHandler;
        idleTimer.Start();

        var appIdleId = Guid.NewGuid();
        _timerActions.Add(appIdleId, new TimerAction(idleTimer, TimerTickHandler));

        Log.Logger?.Information("Idle monitor started with ID: {AppIdleId}", appIdleId);
        return appIdleId;
    }

    /// <summary>
    /// Stops and removes the idle monitoring associated with the specified identifier.
    /// </summary>
    /// <param name="appIdleId">The unique identifier returned by StartAppIdle.</param>
    public void StopAppIdle(Guid appIdleId)
    {
        Log.Logger?.Debug("StopAppIdle: stopping monitor with ID: {AppIdleId}", appIdleId);

        if (_timerActions.TryGetValue(appIdleId, out var timerAction))
        {
            timerAction.Timer.Stop();
            timerAction.Timer.Tick -= timerAction.TimerTickHandler;

            _timerActions.Remove(appIdleId);
            Log.Logger?.Information("Idle monitor stopped and removed: {AppIdleId}", appIdleId);
        }
        else
        {
            Log.Logger?.Warning("StopAppIdle: no monitor found with ID: {AppIdleId}", appIdleId);
        }
    }
}
