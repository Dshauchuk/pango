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

    public FileRepositoryBase(IContentEncoder contentEncoder, IAppDomainProvider appDomainProvider, IAppOptions appOptions, IUserContextProvider userContextProvider, ILogger logger)
    {
        _contentEncoder = contentEncoder;
        _appDomainProvider = appDomainProvider;
        _appOptions = appOptions;
        _userContextProvider = userContextProvider;
        _logger = logger;
    }

    protected abstract string DirectoryName { get; }

    protected async Task<IEnumerable<T>> ExtractAllItemsForUserAsync()
    {
        string userId = _userContextProvider.GetUserName();
        List<T> items = new();

        foreach(string file in ListRepositoryFiles())
        {
            var package = await ReadDataPackageAsync(file);
            
            var t = package.Data as IEnumerable<T>;

            items.AddRange(t);
        }



        return items;
        //string filePath = BuildPath(userId);
        //byte[] encryptedFileContent = await ReadFileContentAsync(filePath);

        //return await _contentEncoder.DecryptAsync<IEnumerable<T>>(encryptedFileContent) ?? Enumerable.Empty<T>();
    }

    protected async Task SaveItemsForUserAsync(IEnumerable<T> items)
    {
        string userId = _userContextProvider.GetUserName();

        IEnumerable<FileContent> contentParts = PrepareContent(items);

        int packageIndex = 1;
        foreach(FileContent contentPart in contentParts)
        {
            string filePath = BuildPath(userId, $"{contentPart.Id}_part{packageIndex}{DefineFileExtension()}");
            await WriteDataPackageAsync(contentPart, filePath);
        }
    }

    #region Private Methods

    private async Task<IEnumerable<T>> ProcessDataPackage(FileContent fileContent)
    {

    }

    private async Task<FileContent> ReadDataPackageAsync(string filePath)
    {
        try
        {
            byte[] encryptedFileContent = await ReadFileContentAsync(filePath);
            var package = await _contentEncoder.DecryptAsync<FileContent>(encryptedFileContent);

            return package;
        }
        catch (Exception ex)
        {

        }
    }

    private async Task WriteDataPackageAsync(FileContent package, string filePath)
    {
        byte[] content = await _contentEncoder.EncryptAsync(package);
        await WriteFileContentAsync(filePath, content);
    }

    private string BuildPath(string userId, string? fileName = null)
        => Path.Combine(_appDomainProvider.GetAppDataFolderPath(), "users", userId, DirectoryName, fileName);

    private IEnumerable<FileContent> PrepareContent<TContent>(IEnumerable<TContent> items)
    {
        List<FileContent> fileContents = new(10);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (var chunk in items.ToList().ChunkBy(DefineCountOfItemsPerFile()))
        {
            FileContent fileContent = new(_userContextProvider.GetUserName(), DefineContentType(), chunk.GetType().FullName, chunk.Count, chunk, now);
            fileContents.Add(fileContent);
        }

        return fileContents;
    }

    private IEnumerable<string> ListRepositoryFiles()
    {
        string directoryPath = BuildPath(_userContextProvider.GetUserName());
        return Directory.GetFiles(directoryPath, $"*{DefineFileExtension()}");
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
