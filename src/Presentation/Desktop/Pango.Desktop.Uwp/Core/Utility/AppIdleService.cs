using Microsoft.UI.Xaml;
using Pango.Desktop.Uwp.Core.Utility.Contracts;
using Pango.Desktop.Uwp.Models;
using System;
using System.Collections.Generic;
using Windows.UI.Core;

namespace Pango.Desktop.Uwp.Core.Utility;

public class AppIdleService : IAppIdleService
{
    private readonly Dictionary<Guid, TimerAction> _timerActions = new();

    /// <inheritdoc/>
    public Guid StartAppIdle(TimeSpan timeOfIdle, Action onIdle)
    {
        EventHandler<object> timerTickHandler = new((_, _) => onIdle());
        DispatcherTimer idleTimer = new()
        {
            Interval = timeOfIdle
        };
        idleTimer.Tick += timerTickHandler;

        Guid appIdleId = Guid.NewGuid();
        _timerActions.Add(appIdleId, new TimerAction(idleTimer, timerTickHandler));

        if (_timerActions.Count == 1)
        {
            Window.Current.CoreWindow.PointerMoved += OnCoreWindowPointerMoved;
            Window.Current.CoreWindow.KeyDown += OnCoreWindowKeyDown;
        }

        return appIdleId;
    }

    /// <inheritdoc/>
    public void StopAppIdle(Guid appIdleId)
    {
        TimerAction timerActionToStop = _timerActions[appIdleId];
        if (timerActionToStop == null)
            return;

        timerActionToStop.Timer.Stop();
        timerActionToStop.Timer.Tick -= timerActionToStop.TimerTickHadler;

        if (_timerActions.Count == 1)
        {
            Window.Current.CoreWindow.PointerMoved -= OnCoreWindowPointerMoved;
            Window.Current.CoreWindow.KeyDown -= OnCoreWindowKeyDown;
        }

        _timerActions.Remove(appIdleId);
    }

    /// <summary>
    /// Restarts all timers imtervals of the <see cref="_timerActions"/> list
    /// </summary>
    private void RestartAllTimers()
    {
        foreach (TimerAction timerAction in _timerActions.Values)
        {
            timerAction.Timer.Stop();
            timerAction.Timer.Start();
        }
    }

    private void OnCoreWindowPointerMoved(CoreWindow sender, PointerEventArgs args)
    {
        RestartAllTimers();
    }

    private void OnCoreWindowKeyDown(CoreWindow sender, KeyEventArgs args)
    {
        RestartAllTimers();
    }
}