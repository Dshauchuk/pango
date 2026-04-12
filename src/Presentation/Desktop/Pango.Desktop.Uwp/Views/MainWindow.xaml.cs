using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Pango.Desktop.Uwp.Core;
using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Core.Utility;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;
using Pango.Desktop.Uwp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Pango.Desktop.Uwp.Views;

/// <summary>
/// Main application window holding the shell and handling OS-level interactions like System Tray.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const int MinWindowWidth = 1050;
    private const int MinWindowHeight = 600;

    private bool _isForceExit = false;
    private H.NotifyIcon.TaskbarIcon? _trayIcon;
    private MenuFlyoutItem? _trayMenuOpenPango;
    private MenuFlyoutItem? _trayMenuClose;
    private MenuFlyoutItem? _trayMenuStartBackup;
    private MenuFlyoutItem? _trayMenuStopBackup;
    private MenuFlyoutItem? _trayMenuExit;
    private readonly ILogger<MainWindow>? _logger;
    private Window? _expirationWindow;
    private string? _currentLoggedInUser;

    public TrayIconViewModel TrayViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        // Setup Logger via Dependency Injection
        _logger = App.Host?.Services?.GetService<ILogger<MainWindow>>();
        var trayLogger = App.Host?.Services?.GetService<ILogger<TrayIconViewModel>>();

        SubClassing();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarBorder);

#if DEBUG
            WindowTitle.Text = Title = $"Pango v.{GetAppVersion()}-dev";
#else
        WindowTitle.Text = Title = $"Pango v.{GetAppVersion()}";
#endif

        TrayViewModel = new TrayIconViewModel(this, trayLogger);
        InitializeSystemTray();
        AppWindow.Closing += AppWindow_Closing;

        RegisterMessengers();

        RootGrid.Loaded += RootGrid_Loaded;
        RootGrid.PointerMoved += RootGrid_PointerMoved;
        RootGrid.KeyDown += RootGrid_KeyDown;

        App.Current.LoginSucceeded += Current_LoginSucceeded;
        App.Current.SignedOut += Current_SignedOut;

        InitializeInitialBackupState();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            CheckAndShowExpirationWindow();
        };
        timer.Start();
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        RootGrid.Loaded -= RootGrid_Loaded;
        CheckAndShowExpirationWindow();
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

        WeakReferenceMessenger.Default.Register<InAppNotificationMessage>(this, (r, m) =>
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (m.Message == "SHOW_EXPIRATION_WINDOW")
            {
                DispatcherQueue.TryEnqueue(() => RefreshExpirationWindow(options, forceShow: true));
            }
            else if (m.Message == "CACHE_UPDATED" && _expirationWindow != null)
            {
                DispatcherQueue.TryEnqueue(() => RefreshExpirationWindow(options, forceShow: true));
            }
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

    private static string GetAppVersion() => AppVersionFormatter.GetDisplayVersion(Assembly.GetEntryAssembly());

    private void Current_SignedOut()
    {
        UserInfo_TitleBar.Visibility = Visibility.Collapsed;
        UserName_TitleBar.Text = string.Empty;
        _currentLoggedInUser = null;

        if (_expirationWindow != null)
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            RefreshExpirationWindow(options, forceShow: true);
        }
    }

    private void Current_LoginSucceeded(string userName)
    {
        UserInfo_TitleBar.Visibility = Visibility.Visible;
        UserName_TitleBar.Text = userName;
        _currentLoggedInUser = userName;

        if (_expirationWindow != null)
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            RefreshExpirationWindow(options, forceShow: true);
        }
    }

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e) => PointerMoved?.Invoke(sender, e);
    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e) => KeyDown?.Invoke(sender, e);

    private void CheckAndShowExpirationWindow()
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        bool alertsEnabled = localSettings.Values[Constants.Settings.EnableExpirationAlerts] as bool? ?? true;
        bool showOnStartup = localSettings.Values[Constants.Settings.ShowExpirationWindowOnStartup] as bool? ?? true;

        if (!alertsEnabled || !showOnStartup) return;

        RefreshExpirationWindow(GetOptions());
    }

    private static System.Text.Json.JsonSerializerOptions GetOptions()
    {
        return new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    private void RefreshExpirationWindow(System.Text.Json.JsonSerializerOptions options, bool forceShow = false)
    {
        string cacheFile = System.IO.Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "expiration_cache.json");
        if (!System.IO.File.Exists(cacheFile) && !forceShow) return;

        try
        {
            Dictionary<string, List<ExpirationCacheItem>> cache = [];
            if (System.IO.File.Exists(cacheFile))
            {
                string json = System.IO.File.ReadAllText(cacheFile).Trim();
                if (json.StartsWith('{') && json.EndsWith('}'))
                {
                    try { cache = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<ExpirationCacheItem>>>(json, options) ?? []; }
                    catch { System.IO.File.Delete(cacheFile); }
                }
                else { System.IO.File.Delete(cacheFile); }
            }

            if (cache.Count == 0 && !forceShow) return;

            int warningDays = Windows.Storage.ApplicationData.Current.LocalSettings.Values[Constants.Settings.ExpirationWarningDays] as int? ?? 7;
            var now = DateTimeOffset.UtcNow.Date;

            var stackPanel = new StackPanel { Spacing = 12 };
            bool hasAnyExpiring = false;
            int visualItemsCount = 0;
            var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();

            var textPrimaryBrush = (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorPrimaryBrush"];
            var textSecondaryBrush = (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorSecondaryBrush"];
            var cardBackgroundBrush = (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
            var cardBorderBrush = (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["CardStrokeColorDefaultBrush"];

            if (!string.IsNullOrEmpty(_currentLoggedInUser) && cache.TryGetValue(_currentLoggedInUser, out var userItems))
            {
                var expiringItems = userItems.Where(i => (i.Date.LocalDateTime.Date - now).TotalDays <= warningDays).OrderBy(i => i.Date).ToList();
                if (expiringItems.Count > 0)
                {
                    hasAnyExpiring = true;
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = string.Format(resourceLoader.GetString("Expiring_Detail_Header"), _currentLoggedInUser),
                        FontSize = 20,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = textPrimaryBrush,
                        Margin = new Thickness(0, 0, 0, 12)
                    });

                    foreach (var item in expiringItems)
                    {
                        visualItemsCount++;
                        int daysLeft = (int)(item.Date.LocalDateTime.Date - now).TotalDays;
                        string statusText;
                        if (daysLeft < 0) statusText = string.Format(resourceLoader.GetString("Expiring_Detail_Expired"), item.Name, -daysLeft);
                        else if (daysLeft == 0) statusText = string.Format(resourceLoader.GetString("Expiring_Detail_ExpiresToday"), item.Name);
                        else statusText = string.Format(resourceLoader.GetString("Expiring_Detail_Expiring"), item.Name, daysLeft);

                        var color = daysLeft <= 0 ? Colors.Red : Colors.DarkOrange;

                        var itemCard = new Border
                        {
                            Background = cardBackgroundBrush,
                            BorderBrush = cardBorderBrush,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(16),
                            Margin = new Thickness(0, 0, 0, 6)
                        };

                        var itemPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
                        itemPanel.Children.Add(new FontIcon { Glyph = "\uE814", FontSize = 18, Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(color), VerticalAlignment = VerticalAlignment.Center });
                        itemPanel.Children.Add(new TextBlock { Text = statusText, FontSize = 16, Foreground = textPrimaryBrush, VerticalAlignment = VerticalAlignment.Center });
                        itemCard.Child = itemPanel;

                        stackPanel.Children.Add(itemCard);
                    }
                }
            }
            else if (string.IsNullOrEmpty(_currentLoggedInUser))
            {
                foreach (var kvp in cache)
                {
                    var expiringCount = kvp.Value.Count(i => (i.Date.LocalDateTime.Date - now).TotalDays <= warningDays && (i.Date.LocalDateTime.Date - now).TotalDays >= 0);
                    var expiredCount = kvp.Value.Count(i => (i.Date.LocalDateTime.Date - now).TotalDays < 0);

                    if (expiringCount > 0 || expiredCount > 0)
                    {
                        hasAnyExpiring = true;
                        visualItemsCount++;

                        var userPanel = new StackPanel
                        {
                            Background = cardBackgroundBrush,
                            Padding = new Thickness(20),
                            CornerRadius = new CornerRadius(8),
                            Margin = new Thickness(0, 0, 0, 12),
                            BorderBrush = cardBorderBrush,
                            BorderThickness = new Thickness(1)
                        };

                        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 8) };
                        headerPanel.Children.Add(new FontIcon { Glyph = "\uE77B", FontSize = 20, Foreground = textPrimaryBrush, VerticalAlignment = VerticalAlignment.Center });
                        headerPanel.Children.Add(new TextBlock { Text = string.Format(resourceLoader.GetString("Expiring_Summary_Header"), kvp.Key), FontSize = 18, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = textPrimaryBrush, VerticalAlignment = VerticalAlignment.Center });
                        userPanel.Children.Add(headerPanel);

                        if (expiringCount > 0) userPanel.Children.Add(new TextBlock { Text = string.Format(resourceLoader.GetString("Expiring_Summary_Expiring"), expiringCount), FontSize = 15, Margin = new Thickness(30, 4, 0, 0), Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.DarkOrange) });
                        if (expiredCount > 0) userPanel.Children.Add(new TextBlock { Text = string.Format(resourceLoader.GetString("Expiring_Summary_Expired"), expiredCount), FontSize = 15, Margin = new Thickness(30, 4, 0, 0), Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Red) });

                        stackPanel.Children.Add(userPanel);
                    }
                }

                if (hasAnyExpiring)
                {
                    visualItemsCount++;
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = resourceLoader.GetString("Expiring_Summary_LoginPrompt"),
                        FontSize = 15,
                        FontStyle = Windows.UI.Text.FontStyle.Italic,
                        Foreground = textSecondaryBrush,
                        TextWrapping = TextWrapping.Wrap,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 16, 0, 0)
                    });
                }
            }

            if (hasAnyExpiring || forceShow)
            {
                if (stackPanel.Children.Count == 0 && forceShow)
                {
                    visualItemsCount = 1;
                    stackPanel.Children.Add(new TextBlock
                    {
                        Text = resourceLoader.GetString("NoPasswordsFound") ?? "No passwords require attention.",
                        FontSize = 15,
                        FontStyle = Windows.UI.Text.FontStyle.Italic,
                        Foreground = textSecondaryBrush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 0)
                    });
                }
                ShowOrUpdateAlertWindow(stackPanel, visualItemsCount);
            }
            else
            {
                _expirationWindow?.Close();
                _expirationWindow = null;
            }
        }
        catch (Exception ex) { _logger?.LogError(ex, "Failed to refresh expiration window."); }
    }

    private void ShowOrUpdateAlertWindow(StackPanel contentPanel, int itemCount)
    {
        int calculatedHeight = Math.Clamp(180 + (itemCount * 85), 240, 600);

        if (_expirationWindow != null)
        {
            if (_expirationWindow.Content is Grid root && root.Children.Count > 1 && root.Children[1] is ScrollViewer sv)
            {
                sv.Content = contentPanel;
            }
            var existingHwnd = WinRT.Interop.WindowNative.GetWindowHandle(_expirationWindow);
            NativeMethods.SetForegroundWindow(existingHwnd);
            _expirationWindow.Activate();
            return;
        }

        try
        {
            var resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();

            _expirationWindow = new Window
            {
                Title = "Pango: Expiration Alert",
                ExtendsContentIntoTitleBar = true
            };

            var rootGrid = new Grid
            {
                Background = (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["ApplicationPageBackgroundThemeBrush"]
            };

            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var titleBar = new Border { Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Transparent) };
            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Padding = new Thickness(16, 0, 0, 0) };
            titlePanel.Children.Add(new Image { Source = new BitmapImage(new Uri("ms-appx:///Assets/logo.png")), Width = 18, Height = 18, VerticalAlignment = VerticalAlignment.Center });
            titlePanel.Children.Add(new TextBlock { Text = "Pango: Expiration Alert", VerticalAlignment = VerticalAlignment.Center, FontSize = 14, Foreground = (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorPrimaryBrush"] });
            titleBar.Child = titlePanel;
            Grid.SetRow(titleBar, 0);
            rootGrid.Children.Add(titleBar);

            var scrollViewer = new ScrollViewer { Margin = new Thickness(32, 10, 32, 20), Content = contentPanel };
            Grid.SetRow(scrollViewer, 1);
            rootGrid.Children.Add(scrollViewer);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 16, Margin = new Thickness(0, 0, 32, 32) };

            var openPangoBtn = new Button
            {
                Content = resourceLoader.GetString("Tray_OpenPango") ?? "Open Pango",
                Style = (Style)Microsoft.UI.Xaml.Application.Current.Resources["AccentButtonStyle"],
                Width = 160,
                Height = 36,
                FontSize = 15
            };
            openPangoBtn.Click += (s, e) => { ShowWindow(); _expirationWindow.Close(); };

            var closeBtn = new Button
            {
                Content = resourceLoader.GetString("Cancel") ?? "Close",
                Width = 120,
                Height = 36,
                FontSize = 15
            };
            closeBtn.Click += (s, e) => _expirationWindow.Close();

            btnPanel.Children.Add(openPangoBtn);
            btnPanel.Children.Add(closeBtn);

            Grid.SetRow(btnPanel, 2);
            rootGrid.Children.Add(btnPanel);

            _expirationWindow.Content = rootGrid;
            _expirationWindow.SetTitleBar(titleBar);

            _expirationWindow.Closed += (s, e) =>
            {
                if (_alertOldWndProc != IntPtr.Zero)
                {
                    var hwndAlert = WinRT.Interop.WindowNative.GetWindowHandle(_expirationWindow);
                    NativeMethods.RestoreWindowLong(hwndAlert, PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC, _alertOldWndProc);
                    _alertOldWndProc = IntPtr.Zero;
                    _alertNewWndProc = null;
                }
                _expirationWindow = null;
            };

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_expirationWindow);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            int width = 550;
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
            if (displayArea != null)
            {
                int x = (displayArea.WorkArea.Width - width) / 2;
                int y = (displayArea.WorkArea.Height - calculatedHeight) / 2;
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, calculatedHeight));
            }
            else
            {
                appWindow.Resize(new Windows.Graphics.SizeInt32(width, calculatedHeight));
            }

            string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "logo.ico");
            if (System.IO.File.Exists(iconPath)) appWindow.SetIcon(iconPath);

            SubClassAlertWindow(hwnd);

            _expirationWindow.Activate();
            NativeMethods.SetForegroundWindow(hwnd);
        }
        catch (Exception ex) { _logger?.LogError(ex, "Failed to create Alert Window."); }
    }

    #region Handle MINMAXINFO

    // DS
    // Jun-28-2024
    // Handling MINMAXINFO allows to set min size of the window
    // see https://github.com/microsoft/microsoft-ui-xaml/issues/2945
    internal delegate IntPtr WinProc(IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);

    internal WinProc? newWndProc = null;
    internal IntPtr oldWndProc = IntPtr.Zero;

    private WinProc? _alertNewWndProc = null;
    private IntPtr _alertOldWndProc = IntPtr.Zero;

#pragma warning disable SYSLIB1054
    [DllImport("user32.dll")]
    static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam);
#pragma warning restore SYSLIB1054

    private void SubClassing()
    {
        var windowId = AppWindow.Id;
        var hwnd = Win32Interop.GetWindowFromWindowId(windowId);

        if (hwnd == IntPtr.Zero) return;

        newWndProc = new(NewWindowProc);
        oldWndProc = NativeMethods.SetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
    }

    private void SubClassAlertWindow(IntPtr hwnd)
    {
        _alertNewWndProc = new(AlertWindowProc);
        _alertOldWndProc = NativeMethods.SetWindowLong(hwnd, PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC, _alertNewWndProc);
    }

    internal static class NativeMethods
    {
#pragma warning disable SYSLIB1054
        // We have to handle the 32-bit and 64-bit functions separately.
        // 'SetWindowLongPtr' is the 64-bit version of 'SetWindowLong', and isn't available in user32.dll for 32-bit processes.
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLong64(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, IntPtr newProc);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, IntPtr newProc);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);
#pragma warning restore SYSLIB1054

        internal static IntPtr SetWindowLong(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc)
        {
            if (IntPtr.Size == 4) // 32-bit process
                return SetWindowLong32(hWnd, nIndex, newProc);
            else // 64-bit process
                return SetWindowLong64(hWnd, nIndex, newProc);
        }

        internal static IntPtr RestoreWindowLong(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, IntPtr oldProc)
        {
            if (IntPtr.Size == 4) return SetWindowLongPtr32(hWnd, nIndex, oldProc);
            else return SetWindowLongPtr64(hWnd, nIndex, oldProc);
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

    private IntPtr AlertWindowProc(IntPtr hWnd, PInvoke.User32.WindowMessage Msg, IntPtr wParam, IntPtr lParam)
    {
        if (Msg == PInvoke.User32.WindowMessage.WM_GETMINMAXINFO)
        {
            var dpi = PInvoke.User32.GetDpiForWindow(hWnd);
            float scalingFactor = (float)dpi / 96;

            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            minMaxInfo.ptMinTrackSize.x = (int)(450 * scalingFactor);
            minMaxInfo.ptMinTrackSize.y = (int)(240 * scalingFactor);
            Marshal.StructureToPtr(minMaxInfo, lParam, true);
        }
        return CallWindowProc(_alertOldWndProc, hWnd, Msg, wParam, lParam);
    }

    #endregion
}