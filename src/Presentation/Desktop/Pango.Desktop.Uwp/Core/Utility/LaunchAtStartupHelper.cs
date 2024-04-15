using Microsoft.Win32;

namespace Pango.Desktop.Uwp.Core.Utility;

internal class LaunchAtStartupHelper
{
    private const string AppRegistryKey = "Pango";

    /// <summary>
    /// Applies setting if the app will be launched at Windows startup
    /// </summary>
    /// <param name="value">If true - launches app at Windows startup, if false - does not launch app at the startup</param>
    internal static void SetLaunchAtStartup(bool value)
    {
        RegistryKey registryKey = GetRegistryKey();

        if (value)
        {
            registryKey.SetValue(AppRegistryKey, System.Reflection.Assembly.GetExecutingAssembly().Location);
        }
        else if (registryKey.GetValue(AppRegistryKey) != null)
        {
            registryKey.DeleteValue(AppRegistryKey);
        }
    }

    /// <summary>
    /// Returns current setting of whether the app should be loaded at the Windows startup
    /// </summary>
    internal static bool GetLaunchAtStartup()
    {
        return GetRegistryKey().GetValue(AppRegistryKey) != null;
    }

    /// <summary>
    /// Builds key of the Windows startup registry section
    /// </summary>
    /// <returns></returns>
    private static RegistryKey GetRegistryKey()
    {
        return Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
    }
}