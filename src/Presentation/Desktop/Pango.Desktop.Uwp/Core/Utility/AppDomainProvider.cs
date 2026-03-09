using Pango.Application.Common;
using Pango.Persistence;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Pango.Desktop.Uwp.Core.Utility;

public class AppDomainProvider : IAppDomainProvider
{
    private string? _cachedCustomPath;
    private bool _customPathResolved;
    public string GetAppDataFolderPath()
    {
        if(_customPathResolved && _cachedCustomPath != null)
        {
            return _cachedCustomPath;
        }

        return ApplicationData.Current.RoamingFolder.Path;
    }

    public async Task<string?> TryGetCustomDataFolderPathAsync()
    {
        try
        {
            var futureAccessList = StorageApplicationPermissions.FutureAccessList;

            if (futureAccessList.ContainsItem(Constants.Settings.CustomDataFolderToken))
            {
                var folder = await futureAccessList.GetFolderAsync(Constants.Settings.CustomDataFolderToken);
                _cachedCustomPath = folder.Path;
                _customPathResolved = true;
                return folder.Path;
            }
        }
        catch
        {
            // folder deleted or is not available
            _cachedCustomPath = null;
            _customPathResolved= true;
        }

        return null;
    }

    public async Task InitializeAsync()
    {
        await TryGetCustomDataFolderPathAsync();
    }

    public string GetTempFolderPath()
    {
        // Get the path to the temporary folder
        StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
        return tempFolder.Path;
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
        => Path.Combine(GetAppDataFolderPath(), AppConstants.UsersFolderName, userName);    
}
