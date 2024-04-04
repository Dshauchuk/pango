using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Extensions;
using Pango.Application.Common.Interfaces;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;

namespace Pango.Persistence;

public abstract class FileRepositoryBase<T>
{
    private readonly IContentEncoder _contentEncoder;
    private readonly IAppDomainProvider _appDomainProvider;
    private readonly IAppOptions _appOptions;
    private readonly IUserContextProvider _userContextProvider;
    private readonly ILogger _logger; 
    private SemaphoreSlim _semaphore = new SemaphoreSlim(1);

    public FileRepositoryBase(IContentEncoder contentEncoder, 
        IAppDomainProvider appDomainProvider, 
        IAppOptions appOptions, 
        IUserContextProvider userContextProvider, 
        ILogger logger)
    {
        _contentEncoder = contentEncoder;
        _appDomainProvider = appDomainProvider;
        _appOptions = appOptions;
        _userContextProvider = userContextProvider;
        _logger = logger;
    }

    #region Properties

    protected abstract string DirectoryName { get; }

    protected IUserContextProvider UserContextProvider => _userContextProvider; 

    #endregion

    #region Methods

    protected async Task<IEnumerable<T>> ExtractAllItemsForUserAsync()
    {
        List<T> items = new();
        IEnumerable<string> files = ListRepositoryFiles();

        foreach (string file in files)
        {
            var package = await ReadDataPackageAsync(file);
            
            if(package is null)
            {
                continue;
            }

            items.AddRange(await ProcessDataPackageAsync(package));
        }

        return items;
    }

    protected async Task SaveItemsForUserAsync(IEnumerable<T> items)
    {
        string userId = _userContextProvider.GetUserName();

        IEnumerable<FileContentPackage> contentParts = PrepareContent(items);

        int packageIndex = 1;
        List<string> usedFiles = new();
        foreach(FileContentPackage contentPart in contentParts)
        {
            string filePath = BuildPath(userId, $"{contentPart.Id}_part{packageIndex}{DefineFileExtension()}");
            await WriteDataPackageAsync(contentPart, filePath);
            usedFiles.Add(filePath);
        }

        IEnumerable<string> allFiles = ListRepositoryFiles();
        IEnumerable<string> uselessFiles = allFiles.Except(usedFiles);

        foreach(string fileToRemove in uselessFiles)
        {
            File.Delete(fileToRemove);
        }
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

    private async Task<FileContentPackage?> ReadDataPackageAsync(string filePath)
    {
        try
        {
            byte[] encryptedFileContent = await ReadFileContentAsync(filePath);
            var package = await _contentEncoder.DecryptAsync<FileContentPackage>(encryptedFileContent);

            // todo: verify if the package id matches the file

            return package;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Cannot read data package from \"{filePath}\"", ex);

            return null;
        }
    }

    private async Task WriteDataPackageAsync(FileContentPackage package, string filePath)
    {
        byte[] content = await _contentEncoder.EncryptAsync(package);
        await WriteFileContentAsync(filePath, content);
    }

    private string BuildPath(string userId, string? fileName = null)
        => string.IsNullOrWhiteSpace(fileName) ? 
            Path.Combine(_appDomainProvider.GetAppDataFolderPath(), "users", userId, DirectoryName) : 
            Path.Combine(_appDomainProvider.GetAppDataFolderPath(), "users", userId, DirectoryName, fileName);

    private IEnumerable<FileContentPackage> PrepareContent<TContent>(IEnumerable<TContent> items)
    {
        List<FileContentPackage> fileContents = new(100);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (var chunk in items.ToList().ChunkBy(DefineCountOfItemsPerFile()))
        {
            FileContentPackage fileContent = new(_userContextProvider.GetUserName(), DefineContentType(), chunk.GetType().FullName, chunk.Count, chunk, now);
            fileContents.Add(fileContent);
        }

        return fileContents;
    }

    private IEnumerable<string> ListRepositoryFiles()
    {
        string directoryPath = BuildPath(_userContextProvider.GetUserName());

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            return new List<string>();
        }
        else
        {
            return Directory.GetFiles(directoryPath, $"*{DefineFileExtension()}");
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

    private ContentType DefineContentType()
    {
        return typeof(T).Name switch
        {
            nameof(PangoPassword) => ContentType.Passwords,
            _ => throw new PangoException(ApplicationErrors.Data.CannotDefineContentType, $"Cannot define content type for \"{typeof(T).FullName}\" data type")
        };
    }

    private string DefineFileExtension()
    {
        return ".pngdat";
    }

    private async Task<byte[]> ReadFileContentAsync(string filePath)
    {
        try
        {
            await _semaphore.WaitAsync();

            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.Create(filePath).Dispose();
            }

            byte[] result;
            using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                result = new byte[stream.Length];
                await stream.ReadAsync(result, 0, (int)stream.Length);
            }

            return result;
        }
        catch (Exception ex)
        {
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
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.Create(filePath).Dispose();
            }

            using (FileStream sourceStream = new(filePath, FileMode.Truncate, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(content, 0, content.Length);
            };
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion
}
