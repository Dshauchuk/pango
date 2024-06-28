using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Pango.Desktop.Uwp.Core.Utility.Contracts;
using Pango.Desktop.Uwp.Models;
using System;
using System.Collections.Generic;

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
            App.Current.CurrentWindow!.PointerMoved += OnWindowPointerMoved;
            App.Current.CurrentWindow!.KeyDown += OnWindowKeyDown;
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
            App.Current.CurrentWindow!.PointerMoved -= OnWindowPointerMoved;
            App.Current.CurrentWindow!.KeyDown -= OnWindowKeyDown;
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

    private void OnWindowPointerMoved(object? sender, PointerRoutedEventArgs args)
    {
        RestartAllTimers();
    }

    private void OnWindowKeyDown(object? sender, KeyRoutedEventArgs args)
    {
        RestartAllTimers();
    }
}