using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;

namespace Pango.Persistence.File;

/// <summary>
/// <see cref="UserRepository"/> is a password vault repository and doesn't interact with the file system directly.
/// All user data are stored in the file system, so need a separate file system based repository to interact with user data (remove user data)
/// </summary>
public class UserFileStorageManager: IUserStorageManager
{
    private readonly IPasswordRepository _passwordRepository;
    private readonly IRepositoryContextFactory _repositoryContextFactory;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IContentEncoder _contentEncoder;
    private readonly IAppDomainProvider _appDomainProvider;
    private readonly IAppOptions _appOptions;
    private readonly ILogger<UserFileStorageManager> _logger;

    public UserFileStorageManager(IPasswordRepository passwordRepository,
        IContentEncoder contentEncoder,
        IAppDomainProvider appDomainProvider,
        IAppOptions appOptions,
        IUserContextProvider userContextProvider,
        IRepositoryContextFactory repositoryContextFactory,
        ILogger<UserFileStorageManager> logger)
    {
        _passwordRepository = passwordRepository;
        _contentEncoder = contentEncoder;
        _appDomainProvider = appDomainProvider;
        _appOptions = appOptions;
        _logger = logger;
        _userContextProvider = userContextProvider;
        _repositoryContextFactory = repositoryContextFactory;
    }

    /// <inheritdoc/>
    public Task DeleteAllUserDataAsync(string userId)
        => DeleteDataAsync(userId);

    public async Task EncryptDataWithAsync(string userId, EncodingOptions encodingOptions)
    {
        _logger.LogDebug("Encrypting data of {UserId}...", userId);

        // read data
        var all = await _passwordRepository.QueryAsync((a) => true, _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync()));

        // 1. save data for temp user
        string tmpUser = $"{userId}_{Guid.NewGuid()}_tmp";
        string newFolderPath = _appDomainProvider.GetUserFolderPath(tmpUser);
        await _passwordRepository.CreateAsync(all, _repositoryContextFactory.Create(tmpUser, new EncodingOptions(encodingOptions.Key, encodingOptions.Salt)));
        _logger.LogDebug("Copied data to a temp user {user} folder...", $"{userId}_{Guid.NewGuid()}_tmp");

        // 2. rename existing directory using timestamp
        string currentUserDirectoryPath = _appDomainProvider.GetUserFolderPath(userId);
        string copyUser = $"{userId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        string tmpUserDirectoryPath = currentUserDirectoryPath.Replace(userId, copyUser);
        Directory.Move(currentUserDirectoryPath, tmpUserDirectoryPath);
        _logger.LogDebug("Moved data from {userFolder} folder to {tmpFolder}", currentUserDirectoryPath, tmpUserDirectoryPath);

        // 3. rename the newly created & encrypted folder as actual 
        Directory.Move(newFolderPath, currentUserDirectoryPath);
        _logger.LogDebug("Moved the just encrypted data from temp folder {tmpFolder} to the user folder {userFolder}", newFolderPath, currentUserDirectoryPath);

        // 4. remove tmp user data
        await DeleteAllUserDataAsync(tmpUser);
        _logger.LogDebug("Deleted the folder of temp user {tmpUser}", tmpUser);

        // 5. remove the copy
        await DeleteAllUserDataAsync(copyUser);
        _logger.LogDebug("Deleted the folder of the user copy {copyUser}", copyUser);

        _logger.LogDebug("Encryption of {UserId} user's data completed", userId);
    }

    /// <summary>
    /// Deletes all files in the folder assigned to <paramref name="userName"/>
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    private Task DeleteDataAsync(string userId)
    {
        string userFolderPath = _appDomainProvider.GetUserFolderPath(userId);
        DirectoryInfo directory = new(userFolderPath);

        if (directory.Exists)
        {
            // delete user's directory, all files and subdirectories
            return Task.Run(() => directory.Delete(true));
        }

        return Task.CompletedTask;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}
