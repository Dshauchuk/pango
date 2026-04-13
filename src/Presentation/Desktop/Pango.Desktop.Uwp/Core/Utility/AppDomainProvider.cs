using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Persistence;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace Pango.Desktop.Uwp.Core.Utility;

public class AppDomainProvider(ILogger<AppDomainProvider>? logger = null) : IAppDomainProvider
{
    private string? _cachedCustomPath;
    private bool _customPathResolved;
    private readonly ILogger<AppDomainProvider>? _logger = logger;

    public void ResetCache()
    {
        _cachedCustomPath = null;
        _customPathResolved = false;
    }

    public string GetAppDataFolderPath()
    {
        if (_customPathResolved && _cachedCustomPath != null)
        {
            return _cachedCustomPath;
        }

        try
        {
            string programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string pangoProgramData = Path.Combine(programDataPath, "Pango");

            if (!Directory.Exists(pangoProgramData))
            {
                Directory.CreateDirectory(pangoProgramData);
            }
            return pangoProgramData;
        }
        catch (UnauthorizedAccessException)
        {
            _logger?.LogWarning("No write permissions for ProgramData. Falling back to Documents folder.");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error accessing ProgramData. Falling back to Documents folder.");
        }

        try
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string pangoDocumentsData = Path.Combine(documentsPath, "Pango");

            if (!Directory.Exists(pangoDocumentsData))
            {
                Directory.CreateDirectory(pangoDocumentsData);
            }
            return pangoDocumentsData;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error accessing Documents folder. Using local app data folder.");
            return ApplicationData.Current.LocalFolder.Path;
        }
    }

    public async Task<string?> TryGetCustomDataFolderPathAsync()
    {
        try
        {
            var futureAccessList = StorageApplicationPermissions.FutureAccessList;
            bool hasToken = false;
            foreach (var entry in futureAccessList.Entries)
            {
                if (entry.Token == Constants.Settings.CustomDataFolderToken)
                {
                    hasToken = true;
                    break;
                }
            }

            if (hasToken)
            {
                var folder = await futureAccessList.GetFolderAsync(Constants.Settings.CustomDataFolderToken);
                _cachedCustomPath = folder.Path;
                _customPathResolved = true;
                return folder.Path;
            }
        }
        catch (Exception ex)
        {
            // folder deleted or is not available
            _logger?.LogWarning(ex, "Failed to get custom data folder path. Falling back to default app data folder.");
            _cachedCustomPath = null;
            _customPathResolved = true;
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
        if (pathElements.Length == 0)
        {
            return GetUserFolderPath(userName);
        }

        string[] pathSegments = new string[pathElements.Length + 1];
        pathSegments[0] = GetUserFolderPath(userName);

        for (int i = 1; i < pathSegments.Length; i++)
        {
            pathSegments[i] = pathElements[i - 1];
        }

        return Path.Combine(pathSegments);
    }

    public string GetUserFolderPath(string userName) =>
        Path.Combine(GetAppDataFolderPath(), AppConstants.UsersFolderName, userName);
}
