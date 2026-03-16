using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.ViewModels;

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// Main application window holding the shell and handling OS-level interactions like System Tray.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const int MinWindowWidth = 800;
    private const int MinWindowHeight = 600;

    private bool _isForceExit = false;
    private H.NotifyIcon.TaskbarIcon? _trayIcon;
    private MenuFlyoutItem? _trayMenuOpenPango;
    private MenuFlyoutItem? _trayMenuClose;
    private MenuFlyoutItem? _trayMenuStartBackup;
    private MenuFlyoutItem? _trayMenuStopBackup;
    private MenuFlyoutItem? _trayMenuExit;
    private readonly ILogger<MainWindow>? _logger;

    public TrayIconViewModel TrayViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        // Setup Logger via Dependency Injection
        _logger = App.Host?.Services?.GetService<ILogger<MainWindow>>();
        var trayLogger = App.Host?.Services?.GetService<ILogger<TrayIconViewModel>>();

        SubClassing();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(this.TitleBarBorder);

#if DEBUG
        WindowTitle.Text = Title = $"Pango Debug v.{GetAppVersion()}";
#else
        WindowTitle.Text = Title = $"Pango v.{GetAppVersion()}";
#endif

        TrayViewModel = new TrayIconViewModel(this, trayLogger);
        InitializeSystemTray();

        this.AppWindow.Closing += AppWindow_Closing;

        RegisterMessengers();

        RootGrid.PointerMoved += RootGrid_PointerMoved;
        RootGrid.KeyDown += RootGrid_KeyDown;

        App.Current.LoginSucceeded += Current_LoginSucceeded;
        App.Current.SignedOut += Current_SignedOut;

        InitializeInitialBackupState();
    }

    /// <summary>
    /// Creates and configures the System Tray icon and its context menu entirely in C#.
    /// </summary>
    private void InitializeSystemTray()
    {
        _trayIcon = new H.NotifyIcon.TaskbarIcon
        {
            ToolTipText = "Pango"
        };

        try
        {
            _trayIcon.IconSource = new BitmapImage(new Uri("ms-appx:///assets/logo.ico"));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load tray icon image. Continuing without icon.");
        }

        var contextMenu = new MenuFlyout();

        _trayMenuOpenPango = new MenuFlyoutItem { Command = TrayViewModel.OpenAppCommand };
        _trayMenuClose = new MenuFlyoutItem { Command = TrayViewModel.CloseAppCommand };
        _trayMenuStartBackup = new MenuFlyoutItem { Command = TrayViewModel.StartBackupCommand };
        _trayMenuStopBackup = new MenuFlyoutItem { Command = TrayViewModel.StopBackupCommand };
        _trayMenuExit = new MenuFlyoutItem { Command = TrayViewModel.ExitCommand };

        contextMenu.Items.Add(_trayMenuOpenPango);
        contextMenu.Items.Add(_trayMenuClose);
        contextMenu.Items.Add(new MenuFlyoutSeparator());
        contextMenu.Items.Add(_trayMenuStartBackup);
        contextMenu.Items.Add(_trayMenuStopBackup);
        contextMenu.Items.Add(new MenuFlyoutSeparator());
        contextMenu.Items.Add(_trayMenuExit);

        _trayIcon.ContextFlyout = contextMenu;
        _trayIcon.DoubleClickCommand = TrayViewModel.OpenAppCommand;

        _trayIcon.ForceCreate();

        UpdateTrayMenuTexts();

        TrayViewModel.UIStateChanged += (s, e) => UpdateTrayMenuState();
        UpdateTrayMenuState();
    }

    /// <summary>
    /// Checks OS processes on startup to verify if the Backup Service is already running.
    /// </summary>
    private void InitializeInitialBackupState()
    {
        try
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("Pango.BackupService");
            TrayViewModel.IsBackupRunning = processes.Length > 0;

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            bool isBackupEnabled = localSettings.Values["IsBackupEnabled"] as bool? ?? false;

            if (isBackupEnabled && processes.Length == 0)
            {
                _logger?.LogInformation("Backup was enabled in settings but is not running. Starting automatically...");
                TrayViewModel.StartBackup();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize initial backup state.");
        }
    }

    /// <summary>
    /// Registers handlers for cross-view messages.
    /// </summary>
    private void RegisterMessengers()
    {
        WeakReferenceMessenger.Default.Register<ImportCompletedMessage>(this, (r, m) =>
        {
            WeakReferenceMessenger.Default.Send(new NavigationRequstedMessage(
                new NavigationParameters(AppView.PasswordsIndex, AppView.ExportImport)));
        });

        WeakReferenceMessenger.Default.Register<AppLanguageChangedMessage>(this, (r, m) =>
        {
            TrayViewModel.UpdateTranslations();
            UpdateTrayMenuTexts();
        });
    }

    /// <summary>
    /// Updates UI text strings of the Tray Menu based on current translations.
    /// </summary>
    private void UpdateTrayMenuTexts()
    {
        _trayMenuOpenPango!.Text = TrayViewModel.OpenPangoText;
        _trayMenuClose!.Text = TrayViewModel.ClosePangoText;
        _trayMenuStartBackup!.Text = TrayViewModel.StartBackupText;
        _trayMenuStopBackup!.Text = TrayViewModel.StopBackupText;
        _trayMenuExit!.Text = TrayViewModel.ExitText;
    }

    /// <summary>
    /// Enables or disables tray menu buttons based on the background task state.
    /// </summary>
    private void UpdateTrayMenuState()
    {
        _trayMenuStartBackup!.IsEnabled = !TrayViewModel.IsBackupRunning;
        _trayMenuStopBackup!.IsEnabled = TrayViewModel.IsBackupRunning;
        _trayMenuClose!.IsEnabled = TrayViewModel.IsAppVisible;
    }

    /// <summary>
    /// Intercepts the window close action to minimize it to the system tray instead.
    /// </summary>
    private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (!_isForceExit)
        {
            args.Cancel = true;
            TrayViewModel.CloseAppCommand.Execute(null);
        }
    }

    /// <summary>
    /// Physically kills the application process and removes the tray icon.
    /// </summary>
    public void ForceExit()
    {
        _isForceExit = true;
        _trayIcon?.Dispose();
        Microsoft.UI.Xaml.Application.Current.Exit();
    }

    public void ShowWindow()
    {
        this.AppWindow.Show();
        var hwnd = Win32Interop.GetWindowFromWindowId(this.AppWindow.Id);
        NativeMethods.SetForegroundWindow(hwnd);
        TrayViewModel.IsAppVisible = true;
    }

    public void HideWindow()
    {
        this.AppWindow.Hide();
        TrayViewModel.IsAppVisible = false;
    }

    public event EventHandler<PointerRoutedEventArgs>? PointerMoved;
    public event EventHandler<KeyRoutedEventArgs>? KeyDown;

    private static string GetAppVersion()
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        return version is null ? "undefined" : string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
    }

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

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e) => PointerMoved?.Invoke(sender, e);
    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e) => KeyDown?.Invoke(sender, e);

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

        if (hwnd == IntPtr.Zero) return;

        newWndProc = new(NewWindowProc);
        oldWndProc = NativeMethods.SetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
    }

    internal static class NativeMethods
    {
        // We have to handle the 32-bit and 64-bit functions separately.
        // 'SetWindowLongPtr' is the 64-bit version of 'SetWindowLong', and isn't available in user32.dll for 32-bit processes.
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLong64(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

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
        if (Msg == PInvoke.User32.WindowMessage.WM_GETMINMAXINFO)
        {
            var dpi = PInvoke.User32.GetDpiForWindow(hWnd);
            float scalingFactor = (float)dpi / 96;

            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            minMaxInfo.ptMinTrackSize.x = (int)(MinWindowWidth * scalingFactor);
            minMaxInfo.ptMinTrackSize.y = (int)(MinWindowHeight * scalingFactor);
            Marshal.StructureToPtr(minMaxInfo, lParam, true);
        }
        return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
    }

    #endregion
}