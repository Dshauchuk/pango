using System.Collections.Generic;

namespace Pango.Desktop.Uwp.Models;

public class AppLanguage
{
    public string Name { get; set; }
    public string Locale { get; set; }

    public static IEnumerable<AppLanguage> GetAppLanguageCollection()
    {
        return new AppLanguage[]
        {
            new AppLanguage { Name = "English", Locale = "en-US" },
            new AppLanguage { Name = "Беларуская", Locale = "be-BY" },
            new AppLanguage { Name = "French", Locale = "fr-FR" }
        };
    }
}