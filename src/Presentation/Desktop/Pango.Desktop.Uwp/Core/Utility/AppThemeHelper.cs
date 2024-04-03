using CommunityToolkit.Mvvm.Messaging;
using Pango.Desktop.Uwp.Mvvm.Messages;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;

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
    public static async Task InitializeAsync()
    {
        _theme = LoadThemeFromSettings();
        await SetRequestedThemeAsync();
    }

    /// <summary>
    /// Changes application theme to passed <paramref name="theme"/>
    /// </summary>
    /// <param name="theme">New theme to apply to the application</param>
    /// <returns></returns>
    public static async Task SetThemeAsync(ElementTheme theme)
    {
        _theme = theme;

        await SetRequestedThemeAsync();
        SaveThemeInSettings(Theme);
    }

    private static async Task SetRequestedThemeAsync()
    {
        WeakReferenceMessenger.Default.Send(new AppThemeChangedMessage(_theme));

        await Task.CompletedTask;
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