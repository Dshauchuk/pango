using Pango.Persistence;
using Windows.Storage;

namespace Pango.Desktop.Uwp.Core.Utility;

public class AppDomainProvider : IAppDomainProvider
{
    public string GetAppDataFolderPath()
    {
        return ApplicationData.Current.RoamingFolder.Path;
    }
}
