using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using Windows.Security.Credentials;
using Windows.Storage;

namespace Pango.Desktop.Uwp.Security;

public class SecureUserSession
{
    private const string ResourceName = "Pango.Desktop.Uwp.Session";
    private const string SessionFlagKey = "Pango_SessionExists";

    private static PasswordCredential? _cachedUser;

    private static ILogger<SecureUserSession>? _logger;
    private static ILogger<SecureUserSession>? Logger => _logger ??= App.Host?.Services?.GetService<ILogger<SecureUserSession>>();

    public static void SaveUser(string username)
    {
        try
        {
            var vault = new PasswordVault();
            var credential = new PasswordCredential(ResourceName, username, username);
            vault.Add(credential);

            ApplicationData.Current.LocalSettings.Values[SessionFlagKey] = true;
            _cachedUser = credential;

            Logger?.LogDebug("User session saved for user '{Username}'", username);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to save user session for user '{Username}'", username);
        }
    }

    public static PasswordCredential? GetUser()
    {
        if (_cachedUser != null)
            return _cachedUser;

        var sessionExists = ApplicationData.Current.LocalSettings.Values[SessionFlagKey] as bool? ?? false;
        if (!sessionExists) return null;

        var vault = new PasswordVault();
        try
        {
            var credentials = vault.FindAllByResource(ResourceName);
            PasswordCredential? credential = credentials.FirstOrDefault();

            if (credential != null)
            {
                credential.RetrievePassword();
                _cachedUser = credential;
                Logger?.LogDebug("Active user session found for '{Username}'", credential.UserName);
                return credential;
            }
        }
        catch (COMException)
        {
            ApplicationData.Current.LocalSettings.Values[SessionFlagKey] = false;
        }

        return null;
    }

    public static void ClearUser()
    {
        _cachedUser = null;

        var sessionExists = ApplicationData.Current.LocalSettings.Values[SessionFlagKey] as bool? ?? false;
        if (!sessionExists) return;

        var vault = new PasswordVault();
        try
        {
            var credentials = vault.FindAllByResource(ResourceName);
            foreach (var credential in credentials)
            {
                vault.Remove(credential);
            }
        }
        catch { }
        finally
        {
            ApplicationData.Current.LocalSettings.Values[SessionFlagKey] = false;
        }
    }
}
