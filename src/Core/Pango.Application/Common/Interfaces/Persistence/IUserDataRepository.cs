namespace Pango.Application.Common.Interfaces.Persistence;

public interface IUserDataRepository
{
    /// <summary>
    /// Removes all user data, except for credentials
    /// </summary>
    /// <param name="userDirectoryPath">path to the user's data directory</param>
    Task DeleteAllUserDataAsync(string userDirectoryPath);
}
