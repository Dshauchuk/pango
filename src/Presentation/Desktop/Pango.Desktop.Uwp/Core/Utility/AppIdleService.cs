using Pango.Desktop.Uwp.Core.Utility.Contracts;
using System;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Pango.Desktop.Uwp.Core.Utility;

public class AppIdleService : IAppIdleService
{
    private bool _isIdle;
    private DispatcherTimer _idleTimer;
    private event EventHandler _isIdleChanged;

    public bool IsIdle
    {
        get
        {
            return _isIdle;
        }

        private set
        {
            if (_isIdle != value)
            {
                _isIdle = value;
                _isIdleChanged?.Invoke(App.Current, EventArgs.Empty);
            }
        }
    }

    public void StartAppIdle(TimeSpan timeOfIdle, Action onIdle)
    {
        _idleTimer = new();
        _idleTimer.Interval = timeOfIdle;
        _idleTimer.Tick += OnIdleTimerTick;
        Window.Current.CoreWindow.PointerMoved += OnCoreWindowPointerMoved;
        Window.Current.CoreWindow.KeyDown += OnCoreWindowKeyDown;
    }

    public void StopAppIdle()
    {
        _idleTimer = null;
        _isIdleChanged = null;
    }

    private void OnIdleTimerTick(object sender, object e)
    {
        _idleTimer.Stop();
        IsIdle = true;
    }

    private void OnCoreWindowPointerMoved(CoreWindow sender, PointerEventArgs args)
    {
        _idleTimer.Stop();
        _idleTimer.Start();
        IsIdle = false;
    }

    private void OnCoreWindowKeyDown(CoreWindow sender, KeyEventArgs args)
    {
        _idleTimer.Stop();
        _idleTimer.Start();
        IsIdle = false;
    }
}