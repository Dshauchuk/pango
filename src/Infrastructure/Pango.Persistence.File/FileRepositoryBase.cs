using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Extensions;
using Pango.Application.Common.Interfaces;
using Pango.Domain.Entities;

namespace Pango.Persistence.File;

public abstract class FileRepositoryBase<T>
{
    private readonly IContentEncoder _contentEncoder;
    private readonly IAppOptions _appOptions;
    private readonly ILogger _logger; 
    private SemaphoreSlim _semaphore = new(1);

    public FileRepositoryBase(
        IContentEncoder contentEncoder,
        IAppOptions appOptions,
        ILogger logger)
    {
        _contentEncoder = contentEncoder;
        _appOptions = appOptions;
        _logger = logger;
    }

    #region Properties

    protected abstract string DirectoryName { get; }

    #endregion

    #region Methods

    protected async Task<IEnumerable<T>> ExtractAllItemsForUserAsync(IEnumerable<string> filePaths, EncodingOptions encodingOptions)
    {
        List<T> items = [];

        foreach (string file in filePaths)
        {
            var package = await ReadDataPackageAsync(file, encodingOptions.Key, encodingOptions.Salt);

            if (package is null)
            {
                continue;
            }

            items.AddRange(await ProcessDataPackageAsync(package));
        }

        return items;
    }

    protected async Task<IEnumerable<T>> ExtractAllItemsForUserAsync(string directoryPath, EncodingOptions encodingOptions)
    {
        IEnumerable<string> filePaths = FileRepositoryBase<T>.ListRepositoryFiles(directoryPath);

        return await ExtractAllItemsForUserAsync(filePaths, encodingOptions);
    }

    protected async Task SaveItemsForUserAsync(IEnumerable<T> items, string ownerName, string directoryPath, EncodingOptions encodingOptions)
    {
        IEnumerable<FileContentPackage> contentParts = PrepareContent(items, ownerName);

        int packageIndex = 1;
        List<string> usedFiles = [];
        foreach(FileContentPackage contentPart in contentParts)
        {
            string filePath = Path.Combine(directoryPath, $"{contentPart.Id}_p{packageIndex++}{FileRepositoryBase<T>.DefineFileExtension()}");
            await WriteDataPackageAsync(contentPart, filePath, encodingOptions.Key, encodingOptions.Salt);
            usedFiles.Add(filePath);
        }

        IEnumerable<string> allFiles = FileRepositoryBase<T>.ListRepositoryFiles(directoryPath);
        IEnumerable<string> uselessFiles = allFiles.Except(usedFiles);

        foreach(string fileToRemove in uselessFiles)
        {
            System.IO.File.Delete(fileToRemove);
        }
    }

    /// <summary>
    /// Deletes all files in the folder assigned to <paramref name="userName"/>
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    protected Task DeleteDataAsync(string directoryPath)
    {
        DirectoryInfo directory = new(directoryPath);

        if (directory.Exists)
        {
            // delete user's directory, all files and subdirectories
            return Task.Run(() => directory.Delete(true));
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    private Task<IEnumerable<T>> ProcessDataPackageAsync(FileContentPackage fileContent)
    {
        if(fileContent is null)
        {
            return Task.FromResult(Enumerable.Empty<T>());
        }

        IEnumerable<T> data = fileContent.Data as IEnumerable<T> ?? Enumerable.Empty<T>();

        return Task.FromResult(data);
    }

    private async Task<FileContentPackage?> ReadDataPackageAsync(string filePath, string key, string salt)
    {
        try
        {
            byte[] encryptedFileContent = await ReadFileContentAsync(filePath);
            var package = await _contentEncoder.DecryptAsync<FileContentPackage>(encryptedFileContent, key, salt);

            // todo: verify if the package id matches the file

            return package;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot read data package from \"{filePath}\"", filePath);

            return null;
        }
    }

    private async Task WriteDataPackageAsync(FileContentPackage package, string filePath, string key, string salt)
    {
        byte[] content = await _contentEncoder.EncryptAsync(package, key, salt);
        await WriteFileContentAsync(filePath, content);
    }

    private IEnumerable<FileContentPackage> PrepareContent<TContent>(IEnumerable<TContent> items, string userName)
    {
        List<FileContentPackage> fileContents = new(100);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (var chunk in items.ToList().ChunkBy(DefineCountOfItemsPerFile()))
        {
            FileContentPackage fileContent = new(userName, DefineContentType(), chunk.GetType().FullName ?? string.Empty, chunk.Count, chunk, now);
            fileContents.Add(fileContent);
        }

        return fileContents;
    }

    private static IEnumerable<string> ListRepositoryFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            return [];
        }
        else
        {
            return Directory.GetFiles(folderPath, $"*{FileRepositoryBase<T>.DefineFileExtension()}");
        }
    }

    private async Task<byte[]> ReadFileContentAsync(string filePath)
    {
        try
        {
            await _semaphore.WaitAsync();

            if (!System.IO.File.Exists(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new PangoException(ApplicationErrors.Data.UnkownError, $"An error occurred while reading data: directory \"{filePath}\" cannot be created because of invalid path"));
                System.IO.File.Create(filePath).Dispose();
            }

            byte[] result;
            using (FileStream stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                result = new byte[stream.Length];
                await stream.ReadAsync(result, 0, (int)stream.Length);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while reading data: {Message}", ex.Message);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task WriteFileContentAsync(string filePath, byte[] content)
    {
        await _semaphore.WaitAsync();

        try
        {
            if (!System.IO.File.Exists(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new PangoException(ApplicationErrors.Data.UnkownError, $"An error occurred while saving data: directory \"{filePath}\" cannot be created because of invalid path"));
                System.IO.File.Create(filePath).Dispose();
            }

            using (FileStream sourceStream = new(filePath, FileMode.Truncate, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(content, 0, content.Length);
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while writing data: {Message}", ex.Message);
            throw;
        }
        finally
        {
            _semaphore.Release();
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
