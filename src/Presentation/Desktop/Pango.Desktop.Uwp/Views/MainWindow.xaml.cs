using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Runtime.InteropServices;

namespace Pango.Desktop.Uwp.Views;

public sealed partial class MainWindow : Window
{
    private const int MinWindowWidth = 800;
    private const int MinWindowHeight = 600;

    public MainWindow ()
	{
		InitializeComponent();

        // set the window minimum size
        SubClassing();

        // set the window actual size

        // set the title
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(this.TitleBarBorder);

#if DEBUG
        WindowTitle.Text = Title = "Pango Debug";
#else
        WindowTitle.Text = Title = "Pango";
#endif

        // track user's activity for IDLE
        RootGrid.PointerMoved += RootGrid_PointerMoved;
        RootGrid.KeyDown += RootGrid_KeyDown;

        App.Current.LoginSucceeded += Current_LoginSucceeded;
        App.Current.SignedOut += Current_SignedOut;
    }

    public event EventHandler<PointerRoutedEventArgs>? PointerMoved;
    public event EventHandler<KeyRoutedEventArgs>? KeyDown;

    private void Current_SignedOut()
    {
        UserInfo_TitleBar.Visibility = Visibility.Collapsed;
        UserName_TitleBar.Text = string.Empty;
    }

    private void Current_LoginSucceeded(string userName)
    {
        UserInfo_TitleBar.Visibility = Visibility.Visible;
        UserName_TitleBar.Text = userName;
    }

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        PointerMoved?.Invoke(sender, e);
    }

    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        KeyDown?.Invoke(sender, e);
    }

    #region Handle MINMAXINFO

    // DS
    // Jun-28-2024
    // Handling MINMAXINFO allows to set min size of the window
    // see https://github.com/microsoft/microsoft-ui-xaml/issues/2945

    internal delegate IntPtr WinProc(IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);
    internal WinProc? newWndProc = null;
    internal IntPtr oldWndProc = IntPtr.Zero;
    [DllImport("user32")]
    private static extern IntPtr SetWindowLong(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);
    [DllImport("user32.dll")]
    static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);

    private void SubClassing()
    {
        var windowId = this.AppWindow.Id;
        var hwnd = Win32Interop.GetWindowFromWindowId(windowId);

        if (hwnd == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to get window handler: error code {error}");
        }

        newWndProc = new(NewWindowProc);

        // Here we use the NativeMethods class 👇
        oldWndProc = NativeMethods.SetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
        if (oldWndProc == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to set GWL_WNDPROC: error code {error}");
        }
    }

    internal static class NativeMethods
    {
        // We have to handle the 32-bit and 64-bit functions separately.
        // 'SetWindowLongPtr' is the 64-bit version of 'SetWindowLong', and isn't available in user32.dll for 32-bit processes.
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLong64(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);

        // This does the selection for us, based on the process architecture.
        internal static IntPtr SetWindowLong(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc)
        {
            if (IntPtr.Size == 4) // 32-bit process
            {
                return SetWindowLong32(hWnd, nIndex, newProc);
            }
            else // 64-bit process
            {
                return SetWindowLong64(hWnd, nIndex, newProc);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MINMAXINFO
    {
        public PInvoke.POINT ptReserved;
        public PInvoke.POINT ptMaxSize;
        public PInvoke.POINT ptMaxPosition;
        public PInvoke.POINT ptMinTrackSize;
        public PInvoke.POINT ptMaxTrackSize;
    }

    private IntPtr NewWindowProc(IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam)
    {
        switch (Msg)
        {
            case PInvoke.User32.WindowMessage.WM_GETMINMAXINFO:
                var dpi = PInvoke.User32.GetDpiForWindow(hWnd);
                float scalingFactor = (float)dpi / 96;

                MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                minMaxInfo.ptMinTrackSize.x = (int)(MinWindowWidth * scalingFactor);
                minMaxInfo.ptMinTrackSize.y = (int)(MinWindowHeight * scalingFactor);
                Marshal.StructureToPtr(minMaxInfo, lParam, true);
                break;

        }
        return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
    }

    #endregion
}