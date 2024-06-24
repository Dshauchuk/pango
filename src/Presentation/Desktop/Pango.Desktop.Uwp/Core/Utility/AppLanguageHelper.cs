using CommunityToolkit.Mvvm.Messaging;
using Pango.Desktop.Uwp.Models;
using Pango.Desktop.Uwp.Mvvm.Messages;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Windows.Globalization;
using Windows.Storage;

namespace Pango.Desktop.Uwp.Core.Utility;

public static class AppLanguageHelper
{
    /// <summary>
    /// Returns currently applied to the application language. Null if no information is stored about applied language
    /// </summary>
    public static AppLanguage GetAppliedAppLanguage()
    {
        IEnumerable<AppLanguage> appLanguages = AppLanguage.GetAppLanguageCollection();
        string selectedLanguageLocale = ApplicationData.Current.LocalSettings.Values[Constants.Settings.AppLanguage] as string ?? "en-US";

        return appLanguages.FirstOrDefault(l => l.Locale.Equals(selectedLanguageLocale, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Applies <paramref name="newLanguage"/> to the application. Use at the application startup. 
    /// Use <see cref="ChangeAppLanguage(AppLanguage)"/> to change language at runtime.
    /// </summary>
    /// <param name="newLanguage">Language to apply to the app</param>
    /// <returns>True if app language was changed to the <paramref name="newLanguage"/>. False if <paramref name="newLanguage"/> is invalid or already applied to the app</returns>
    public static bool ApplyApplicationLanguage(AppLanguage newLanguage)
    {
        try
        {
            string selectedLanguageLocale = ApplicationData.Current.LocalSettings.Values[Constants.Settings.AppLanguage] as string;

            if (string.IsNullOrEmpty(newLanguage?.Locale) || (!string.IsNullOrEmpty(selectedLanguageLocale) && selectedLanguageLocale.Equals(newLanguage.Locale, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // apply new language
            CultureInfo ci = new(newLanguage.Locale);
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            ApplicationLanguages.PrimaryLanguageOverride = newLanguage.Locale;
            Windows.ApplicationModel.Resources.Core.ResourceContext.GetForCurrentView().Reset();
            Windows.ApplicationModel.Resources.Core.ResourceContext.GetForViewIndependentUse().Reset();

            // save new language to the local app settings
            ApplicationData.Current.LocalSettings.Values[Constants.Settings.AppLanguage] = newLanguage.Locale;

            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error($"An error occurred while changing language: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Changes current application language at the runtime
    /// </summary>
    /// <param name="newLanguage">Language, that should be applied to the application</param>
    /// <param name="pageType">Type of a page to which application should be redirected after language is changed</param>
    public static void ChangeAppLanguage(AppLanguage newLanguage, Type pageType)
    {
        if (!ApplyApplicationLanguage(newLanguage))
        {
            return;
        }

        WeakReferenceMessenger.Default.Send<AppLanguageChangedMessage>(new(pageType));
    }
}
