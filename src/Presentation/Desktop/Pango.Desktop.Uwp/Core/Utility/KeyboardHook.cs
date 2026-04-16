using Microsoft.UI.Xaml;

namespace Pango.Desktop.Uwp.Core.Utility
{
    public partial class KeyboardHook : IDisposable
    {
        public event EventHandler<bool>? CapsLockChanged;
        private readonly DispatcherTimer _timer;
        private bool _lastState;
        private bool _disposed = false;

        public KeyboardHook()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _lastState = GetCapsLockState();
        }

        private void Timer_Tick(object? sender, object e)
        {
            bool isLocked = GetCapsLockState();
            if (isLocked != _lastState)
            {
                _lastState = isLocked;
                CapsLockChanged?.Invoke(this, isLocked);
            }
        }

        private static bool GetCapsLockState()
        {
            try
            {
                var state = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.CapitalLock);
                return state.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Locked);
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~KeyboardHook()
        {
            Dispose();
        }
    }
}
