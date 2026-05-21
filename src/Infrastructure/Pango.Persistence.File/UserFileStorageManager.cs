using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;

namespace Pango.Persistence.File;

/// <summary>
/// Repository for managing file-based user storage and data migration.
/// </summary>
public class UserFileStorageManager(
    IPasswordRepository passwordRepository,
    IAppDomainProvider appDomainProvider,
    IUserContextProvider userContextProvider,
    IRepositoryContextFactory repositoryContextFactory,
    ILogger<UserFileStorageManager> logger) : IUserStorageManager
{
    private readonly IPasswordRepository _passwordRepository = passwordRepository;
    private readonly IRepositoryContextFactory _repositoryContextFactory = repositoryContextFactory;
    private readonly IUserContextProvider _userContextProvider = userContextProvider;
    private readonly IAppDomainProvider _appDomainProvider = appDomainProvider;
    private readonly ILogger<UserFileStorageManager> _logger = logger;

    private const int FileStreamBufferSize = 4096;

    /// <summary>
    /// Deletes all user data for the specified user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeleteAllUserDataAsync(string userId)
        => DeleteDataAsync(userId);

    /// <summary>
    /// Encrypts all user data with the specified encoding options and generates expiration cache.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="encodingOptions">The encoding options (key and salt).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task EncryptDataWithAsync(string userId, EncodingOptions encodingOptions)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Encrypting data of {UserId}...", userId);

        // Read all current data
        var all = await _passwordRepository.QueryAsync((a) => true, _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync()));

        // 1.save data for temp user
        string tempId = Guid.NewGuid().ToString();
        string tmpUser = $"{userId}_{tempId}_tmp";
        string newFolderPath = _appDomainProvider.GetUserFolderPath(tmpUser);
        await _passwordRepository.CreateAsync(all, _repositoryContextFactory.Create(tmpUser, new EncodingOptions(encodingOptions.Key, encodingOptions.Salt)));


        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Copied data to a temp user {User} folder...", tmpUser);

        // 2. rename existing directory using timestamp
        string currentUserDirectoryPath = _appDomainProvider.GetUserFolderPath(userId);
        string copyUser = $"{userId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        string tmpUserDirectoryPath = currentUserDirectoryPath.Replace(userId, copyUser);
        Directory.Move(currentUserDirectoryPath, tmpUserDirectoryPath);

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Moved data from {UserFolder} folder to {TmpFolder}", currentUserDirectoryPath, tmpUserDirectoryPath);

        // 3. rename the newly created & encrypted folder as actual 
        Directory.Move(newFolderPath, currentUserDirectoryPath);

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Moved the just encrypted data from temp folder {TmpFolder} to the user folder {UserFolder}", newFolderPath, currentUserDirectoryPath);

        // 4. remove tmp user data
        await DeleteAllUserDataAsync(tmpUser);
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Deleted the folder of temp user {TmpUser}", tmpUser);

        // 5. remove the copy
        await DeleteAllUserDataAsync(copyUser);
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Deleted the folder of the user copy {CopyUser}", copyUser);

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Encryption of {UserId} user's data completed", userId);
    }

    /// <summary>
    /// Physically deletes the data directory for the given user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private Task DeleteDataAsync(string userId)
    {
        string userFolderPath = _appDomainProvider.GetUserFolderPath(userId);
        DirectoryInfo directory = new(userFolderPath);

        if (directory.Exists)
        {
            // Delete user's directory, all files and subdirectories
            return Task.Run(() => directory.Delete(true));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Recursively copies a directory to a new location.
    /// </summary>
    /// <param name="sourceDir">The source directory path.</param>
    /// <param name="destinationDir">The destination directory path.</param>
    /// <param name="recursive">Indicates whether to copy subdirectories.</param>
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
            using var sourceStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, FileStreamBufferSize, true);
            using var targetStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None, FileStreamBufferSize, true);
            sourceStream.CopyTo(targetStream);
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

    /// <summary>
    /// Migrates user data from an old base path to a new base path.
    /// </summary>
    /// <param name="oldBasePath">The old base path.</param>
    /// <param name="newBasePath">The new base path.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task MigrateDataAsync(string oldBasePath, string newBasePath)
    {
        string oldUsersDir = Path.Combine(oldBasePath, AppConstants.UsersFolderName);

        if (!Directory.Exists(oldUsersDir))
            return;

        string newUsersDir = Path.Combine(newBasePath, AppConstants.UsersFolderName);

        await Task.Run(() =>
        {
            CopyDirectory(oldUsersDir, newUsersDir, recursive: true);
            Directory.Delete(oldUsersDir, recursive: true);
        });

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Migrated user data from {Old} to {New}", oldUsersDir, newUsersDir);
    }
}
