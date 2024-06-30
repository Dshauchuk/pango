using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Pango.Desktop.Uwp.Core.Utility;

public static class TitleBarHelper
{
    /// <summary>
    /// Styles the title bar buttons according to the theme in use.
    /// </summary>
    public static void StyleTitleBar()
    {
        ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;

        // Transparent colors
        titleBar.ForegroundColor = Colors.Transparent;
        titleBar.BackgroundColor = Colors.Transparent;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.InactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        // Theme aware colors
        titleBar.ButtonForegroundColor = titleBar.ButtonHoverForegroundColor = titleBar.ButtonPressedForegroundColor = Colors.White;
        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF);
        titleBar.ButtonPressedBackgroundColor = Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF);
        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xC0, 0xFF, 0xFF, 0xFF);
        titleBar.InactiveForegroundColor = Color.FromArgb(0xA0, 0xA0, 0xA0, 0xA0);
    }

    /// <summary>
    /// Sets up the app UI to be expanded into the title bar.
    /// </summary>
    public static void ExpandViewIntoTitleBar()
    {
        CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
        coreTitleBar.ExtendViewIntoTitleBar = true;
    }

    public static void SetCaptionButtonColors(Window window, Windows.UI.Color color)
    {
        var res = App.Current.Resources;
        res["WindowCaptionForeground"] = color;
        window.AppWindow.TitleBar.ButtonForegroundColor = color;
    }

    public static void SetCaptionButtonBackgroundColors(Window window, Windows.UI.Color? color)
    {
        var titleBar = window.AppWindow.TitleBar;
        titleBar.ButtonBackgroundColor = color;
    }

    public static void SetForegroundColor(Window window, Windows.UI.Color? color)
    {
        var titleBar = window.AppWindow.TitleBar;
        titleBar.ForegroundColor = color;
    }

    public static void SetBackgroundColor(Window window, Windows.UI.Color? color)
    {
        var titleBar = window.AppWindow.TitleBar;
        titleBar.BackgroundColor = color;
    }
}
