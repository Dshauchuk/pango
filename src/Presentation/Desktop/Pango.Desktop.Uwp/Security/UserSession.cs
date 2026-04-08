using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Windows.Security.Credentials;
using Windows.Storage;

namespace Pango.Desktop.Uwp.Security;

public class SecureUserSession
{
    private const string ResourceName = "Pango.Desktop.Uwp.Session";
    private const string SessionFlagKey = "Pango_SessionExists";

    private static ILogger<SecureUserSession>? _logger;
    private static ILogger<SecureUserSession>? Logger
    {
        get
        {
            if (_logger == null && App.Host != null)
            {
                _logger = App.Host.Services.GetService<ILogger<SecureUserSession>>();
            }
            return _logger;
        }
    }

    public static void SaveUser(string username)
    {
        try
        {
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential(ResourceName, username, username));

            ApplicationData.Current.LocalSettings.Values[SessionFlagKey] = true;

            Logger?.LogDebug("User session saved for user '{Username}'", username);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to save user session for user '{Username}'", username);
        }
    }

    public static PasswordCredential? GetUser()
    {
        var sessionExists = ApplicationData.Current.LocalSettings.Values[SessionFlagKey] as bool? ?? false;
        if (!sessionExists)
        {
            Logger?.LogDebug("No active user session flag found. Skipping PasswordVault query.");
            return null;
        }

        var vault = new PasswordVault();
        try
        {
            var credentials = vault.FindAllByResource(ResourceName);
            PasswordCredential? credential = credentials.FirstOrDefault();

            if (credential != null)
            {
                credential.RetrievePassword();
                Logger?.LogDebug("Active user session found for '{Username}'", credential.UserName);
                return credential;
            }
        }
        catch (COMException ex) when ((uint)ex.HResult == 0x80070490)
        {
            ApplicationData.Current.LocalSettings.Values[SessionFlagKey] = false;
            Logger?.LogDebug("Session flag was true, but vault is empty. Resetting flag.");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "An unexpected error occurred while retrieving the user session.");
        }

        return null;
    }

    public static void ClearUser()
    {
        var sessionExists = ApplicationData.Current.LocalSettings.Values[SessionFlagKey] as bool? ?? false;
        if (!sessionExists) return;

        var vault = new PasswordVault();
        try
        {
            var credentials = vault.FindAllByResource(ResourceName);
            foreach (var credential in credentials)
            {
                vault.Remove(credential);
                Logger?.LogDebug("User session cleared for '{Username}'", credential.UserName);
            }
        }
        catch (COMException ex) when ((uint)ex.HResult == 0x80070490)
        {
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "An unexpected error occurred while clearing the user session.");
        }
        finally
        {
            ApplicationData.Current.LocalSettings.Values[SessionFlagKey] = false;
        }
    }
}
