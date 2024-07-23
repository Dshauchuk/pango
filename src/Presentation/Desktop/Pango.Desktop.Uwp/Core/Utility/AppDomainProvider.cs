using Pango.Persistence;
using System.IO;
using Windows.Storage;

namespace Pango.Desktop.Uwp.Core.Utility;

public class AppDomainProvider : IAppDomainProvider
{
    public string GetAppDataFolderPath()
    {
        return ApplicationData.Current.RoamingFolder.Path;
    }

    public string GetPath(string userName, params string[] pathElements)
    {
        if(pathElements.Length == 0)
        {
            return GetUserFolderPath(userName);
        }

        string[] pathSegments = new string[pathElements.Length + 1];
        pathSegments[0] = GetUserFolderPath(userName);
        for(int i = 1; i < pathSegments.Length; i++)
        {
            pathSegments[i] = pathElements[i - 1];
        }

        return Path.Combine(pathSegments);
    }

    public string GetUserFolderPath(string userName)
        => Path.Combine(GetAppDataFolderPath(), "users", userName);
}
