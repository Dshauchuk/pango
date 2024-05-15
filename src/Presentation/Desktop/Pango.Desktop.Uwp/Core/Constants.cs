namespace Pango.Desktop.Uwp.Core;

public static class Constants
{
    public static class Settings
    {
        /// <summary>
        /// Contains key, by which selected application language can be retrived from the <see cref="Windows.Storage.ApplicationDataContainer"/>
        /// </summary>
        public const string AppLanguage = "AppLanguage";

        /// <summary>
        /// Contains key, by which selected application theme can be retrived from the <see cref="Windows.Storage.ApplicationDataContainer"/>
        /// </summary>
        public const string AppTheme = "AppTheme";

        /// <summary>
        /// Contains key, by which value of idle to block the application can be retrived from the <see cref="Windows.Storage.ApplicationDataContainer"/>
        /// </summary>
        public const string BlockAppAfterIdleMinutes = "BlockAppAfterIdleMinutes";
    }
}
