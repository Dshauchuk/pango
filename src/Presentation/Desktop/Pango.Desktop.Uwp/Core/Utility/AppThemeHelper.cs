using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Pango.Desktop.Uwp.Mvvm.Messages;
using System;
using Windows.Storage;

namespace Pango.Desktop.Uwp.Core.Utility;

internal static class AppThemeHelper
{
    private static ElementTheme _theme;

    static AppThemeHelper()
    {
        _theme = LoadThemeFromSettings();
    }

    /// <summary>
    /// Returns theme that is currently applied to the application
    /// </summary>
    public static ElementTheme Theme
    {
        get { return _theme; }
    }

    /// <summary>
    /// Applies theme to the application on startup
    /// </summary>
    public static void Initialize()
    {
        _theme = LoadThemeFromSettings();
        SetRequestedTheme(_theme);
    }

    /// <summary>
    /// Changes application theme to passed <paramref name="theme"/>
    /// </summary>
    /// <param name="theme">New theme to apply to the application</param>
    /// <returns></returns>
    public static void SetTheme(ElementTheme theme)
    {
        _theme = theme;

        SetRequestedTheme(theme);
        SaveThemeInSettings(Theme);
    }

    private static void SetRequestedTheme(ElementTheme theme)
    {
        if (App.Current.CurrentWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }

        WeakReferenceMessenger.Default.Send(new AppThemeChangedMessage(_theme));
    }

    private static ElementTheme LoadThemeFromSettings()
    {
        var themeName = ApplicationData.Current.LocalSettings.Values[Constants.Settings.AppTheme] as string;

        if (Enum.TryParse(themeName, out ElementTheme cacheTheme))
        {
            return cacheTheme;
        }

        return ElementTheme.Default;
    }

    private static void SaveThemeInSettings(ElementTheme theme)
    {
        ApplicationData.Current.LocalSettings.Values[Constants.Settings.AppTheme] = theme.ToString();
    }
}