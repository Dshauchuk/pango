using System;
using Windows.UI.Xaml;

namespace Pango.Desktop.Uwp.Models;

/// <summary>
/// Class to unit timer with its Tick event handler
/// </summary>
internal class TimerAction
{
    public TimerAction(DispatcherTimer timer, EventHandler<object> timerTickHadler)
    {
        Timer = timer;
        TimerTickHadler = timerTickHadler;
    }

    public DispatcherTimer Timer { get; private set; }
    public EventHandler<object> TimerTickHadler { get; private set; }
}