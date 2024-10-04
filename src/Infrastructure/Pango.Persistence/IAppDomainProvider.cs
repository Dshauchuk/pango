namespace Pango.Persistence;

public interface IAppDomainProvider
{
    /// <summary>
    /// Returns the app data folder path
    /// </summary>
    /// <returns></returns>
    string GetAppDataFolderPath();

    /// <summary>
    /// Returns the path of the folder assigned to <paramref name="userName"/>
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    string GetUserFolderPath(string userName);

    /// <summary>
    /// Returns the path of the folder that's defined by <paramref name="pathElements"/> and located in the folder assigned <paramref name="userName"/>
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="pathElements"></param>
    /// <returns></returns>
    string GetPath(string userName, params string[] pathElements);

    /// <summary>
    /// Returns a path to the temporary folder
    /// </summary>
    /// <returns></returns>
    string GetTempFolderPath();
}
