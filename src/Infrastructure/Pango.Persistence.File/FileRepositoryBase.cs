using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Extensions;
using Pango.Application.Common.Interfaces;
using Pango.Domain.Entities;
using Pango.Domain.Enums;

namespace Pango.Persistence.File;

public abstract class FileRepositoryBase<T>(
    IContentEncoder contentEncoder,
    IAppDomainProvider appDomainProvider,
    IAppOptions appOptions,
    ILogger logger)
{
    private readonly IContentEncoder _contentEncoder = contentEncoder;
    private readonly IAppDomainProvider _appDomainProvider = appDomainProvider;
    private readonly IAppOptions _appOptions = appOptions;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    #region Properties

    protected ILogger Logger { get; init; } = logger;
    protected abstract string DirectoryName { get; }

    #endregion

    #region Methods

    protected async Task<IEnumerable<T>> ExtractAllItemsForUserAsync(IEnumerable<string> filePaths, EncodingOptions encodingOptions)
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<T>();
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

        await Parallel.ForEachAsync(filePaths, parallelOptions, async (file, token) =>
        {
            var package = await ReadDataPackageAsync(file, encodingOptions.Key, encodingOptions.Salt);
            if (package != null)
            {
                var items = await ProcessDataPackageAsync(package);
                foreach (var item in items)
                {
                    results.Add(item);
                }
            }
        });

        return [.. results];
    }

    protected async Task<IEnumerable<T>> ExtractAllItemsForUserAsync(string directoryPath, EncodingOptions encodingOptions)
    {
        IEnumerable<string> filePaths = ListRepositoryFiles(directoryPath);
        return await ExtractAllItemsForUserAsync(filePaths, encodingOptions);
    }

    protected async Task SaveItemsForUserAsync(IEnumerable<T> items, string ownerName, string directoryPath, EncodingOptions encodingOptions)
    {
        IEnumerable<ContentPackage> contentParts = PrepareContent(items, ownerName);
        var contentPartsList = contentParts.Select((part, idx) => new { part, index = idx + 1 }).ToList();
        var usedFilesBag = new System.Collections.Concurrent.ConcurrentBag<string>();
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

        await Parallel.ForEachAsync(contentPartsList, parallelOptions, async (item, token) =>
        {
            string filePath = Path.Combine(directoryPath, $"{item.part.Id}_p{item.index}{DefineFileExtension()}");
            await WriteDataPackageAsync(item.part, filePath, encodingOptions.Key, encodingOptions.Salt);
            usedFilesBag.Add(filePath);
        });

        List<string> usedFiles = [.. usedFilesBag];
        IEnumerable<string> allFiles = ListRepositoryFiles(directoryPath);
        IEnumerable<string> uselessFiles = allFiles.Except(usedFiles);

        foreach (string fileToRemove in uselessFiles)
        {
            System.IO.File.Delete(fileToRemove);
        }
    }

    /// <summary>
    /// Deletes all files in the folder assigned to <paramref name="userName"/>
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    protected Task DeleteDataAsync(string userId)
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

    #endregion

    #region Private Methods

    private static Task<IEnumerable<T>> ProcessDataPackageAsync(ContentPackage fileContent)
    {
        if (fileContent is null) return Task.FromResult(Enumerable.Empty<T>());

        IEnumerable<T> data = fileContent.Data as IEnumerable<T> ?? [];
        return Task.FromResult(data);
    }

    private async Task<ContentPackage?> ReadDataPackageAsync(string filePath, string key, string salt)
    {
        try
        {
            byte[] encryptedFileContent = await ReadFileContentAsync(filePath);
            return await _contentEncoder.DecryptAsync<ContentPackage>(encryptedFileContent, key, salt);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Cannot read data package from \"{filePath}\"", filePath);
            return null;
        }
    }

    private async Task WriteDataPackageAsync(ContentPackage package, string filePath, string key, string salt)
    {
        byte[] content = await _contentEncoder.EncryptAsync(package, key, salt);
        await FileRepositoryBase<T>.WriteFileContentAsync(filePath, content);
    }

    private List<ContentPackage> PrepareContent<TContent>(IEnumerable<TContent> items, string userName)
    {
        List<ContentPackage> fileContents = [];
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (var chunk in items.ToList().ChunkBy(DefineCountOfItemsPerFile()))
        {
            ContentPackage fileContent = new(userName, DefineContentType(), chunk.GetType().AssemblyQualifiedName ?? string.Empty, chunk.Count, chunk, now);
            fileContents.Add(fileContent);
        }

        return fileContents;
    }

    private static string[] ListRepositoryFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            return [];
        }
        return Directory.GetFiles(folderPath, $"*{DefineFileExtension()}");
    }

    private async Task<byte[]> ReadFileContentAsync(string filePath)
    {
        try
        {
            if (!System.IO.File.Exists(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new PangoException(ApplicationErrors.Data.UnknownError, $"An error occurred while reading data: directory \"{filePath}\" cannot be created because of invalid path"));
                System.IO.File.Create(filePath).Dispose();

                return [];
            }

            return await System.IO.File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while reading data: {Message}", ex.Message);
            throw;
        }
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

    private static async Task WriteFileContentAsync(string filePath, byte[] content)
    {
        var fileLock = _fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));

        await fileLock.WaitAsync();
        try
        {
            string tempPath = filePath + ".tmp";
            await using (FileStream sourceStream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await sourceStream.WriteAsync(content);
            }
            System.IO.File.Move(tempPath, filePath, true);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private int DefineCountOfItemsPerFile()
    {
        return DefineContentType() switch
        {
            ContentType.Passwords => _appOptions.FileOptions.PasswordsPerFile,
            _ => AppConstants.DefaultNumberOfItemsPerFile
        };
    }

    /// <summary>
    /// Returns <see cref="ContentType"/> that is corresponding to <see cref="T"/>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="PangoException"></exception>
    private ContentType DefineContentType()
    {
        return typeof(T).Name switch
        {
            nameof(PangoPassword) => ContentType.Passwords,
            _ => throw new PangoException(ApplicationErrors.Data.CannotDefineContentType, $"Cannot define content type for \"{typeof(T).FullName}\" data type")
        };
    }

    /// <summary>
    /// Returns file extension that's used by this repository
    /// </summary>
    /// <returns></returns>
    private static string DefineFileExtension() => ".pngdat";

    #endregion
}
