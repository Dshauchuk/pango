using Microsoft.UI.Xaml;

namespace Pango.Desktop.Uwp.Models;

/// <summary>
/// Class to unit timer with its Tick event handler
/// </summary>
internal class TimerAction(DispatcherTimer timer, EventHandler<object> timerTickHandler)
{
    public DispatcherTimer Timer { get; private set; } = timer;
    public EventHandler<object> TimerTickHandler { get; private set; } = timerTickHandler;
}