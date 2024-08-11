namespace Pango.Application.Common.Interfaces.Persistence;

public interface IUserStorageManager
{
    /// <summary>
    /// Removes all user data, except for credentials
    /// </summary>
    /// <param name="userId">userId</param>
    Task DeleteAllUserDataAsync(string userId);

    Task EncryptDataWithAsync(string userId, EncodingOptions encodingOptions);
}
