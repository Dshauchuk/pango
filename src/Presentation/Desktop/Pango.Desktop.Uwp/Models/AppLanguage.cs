using System.Collections.Generic;

namespace Pango.Desktop.Uwp.Models;

public class AppLanguage
{
    public string Name { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;

    public static IEnumerable<AppLanguage> GetAppLanguageCollection()
    {
        return
        [
            new AppLanguage { Name = "English", Locale = "en-US" },
            new AppLanguage { Name = "Беларуская", Locale = "be-BY" },
        ];
    }
}