using System;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Pango.Desktop.Uwp.Core.Utility;

public static class AppIdleHelper
{
    public static event EventHandler IsIdleChanged;

    private static DispatcherTimer idleTimer;

    static AppIdleHelper()
    {
        int? blockAppAfterIdleMinutes = (int?)ApplicationData.Current.LocalSettings.Values[Constants.Settings.BlockAppAfterIdleMinutes];

        idleTimer = new DispatcherTimer();
        idleTimer.Interval = TimeSpan.FromSeconds(5);  // 10s idle delay
        idleTimer.Tick += onIdleTimerTick;
        Window.Current.CoreWindow.PointerMoved += onCoreWindowPointerMoved;
        Window.Current.CoreWindow.KeyDown += onCoreWindowKeyDown;
    }

    private static bool isIdle;
    public static bool IsIdle
    {
        get
        {
            return isIdle;
        }

        private set
        {
            if (isIdle != value)
            {
                isIdle = value;
                IsIdleChanged?.Invoke(App.Current, EventArgs.Empty);
            }
        }
    }

    private static void onIdleTimerTick(object sender, object e)
    {
        idleTimer.Stop();
        IsIdle = true;
    }

    private static void onCoreWindowPointerMoved(CoreWindow sender, PointerEventArgs args)
    {
        idleTimer.Stop();
        idleTimer.Start();
        IsIdle = false;
    }

    private static void onCoreWindowKeyDown(CoreWindow sender, KeyEventArgs args)
    {
        idleTimer.Stop();
        idleTimer.Start();
        IsIdle = false;
    }
}
