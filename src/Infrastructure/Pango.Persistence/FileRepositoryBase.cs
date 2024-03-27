namespace Pango.Persistence;

public abstract class FileRepositoryBase<T>
{
    private readonly IContentEncoder _contentEncoder;
    private readonly IAppDomainProvider _appDomainProvider;

    private const int RetryCount = 3;
    private SemaphoreSlim semaphore = new SemaphoreSlim(1);

    public FileRepositoryBase(IContentEncoder contentEncoder, IAppDomainProvider appDomainProvider)
    {
        _contentEncoder = contentEncoder;
        _appDomainProvider = appDomainProvider;
    }

    protected abstract string FileName { get; }

    protected async Task<IEnumerable<T>> ExtractAllItemsForUserAsync(string userName)
    {
        string filePath = BuildPath(userName);
        byte[] encryptedFileContent = await ReadFileContentAsync(filePath);

        return await _contentEncoder.DecryptAsync<IEnumerable<T>>(encryptedFileContent) ?? Enumerable.Empty<T>();
    }

    protected async Task SaveItemsForUserAsync(string userName, IEnumerable<T> items)
    {
        string filePath = BuildPath(userName);
        byte[] content = await _contentEncoder.EncryptAsync(items);

        await WriteFileContentAsync(filePath, content);
    }

    protected Task DeleteUserDataAsync(string userName)
    {
        DirectoryInfo directory = new(BuildUserFolderPath(userName));

        if (directory.Exists)
        {
            // delete user's directory, all files and subdirectories
            return Task.Run(() => directory.Delete(true));
        }

        return Task.CompletedTask;
    }

    #region Private Methods

    private string BuildUserFolderPath(string userName)
        => Path.Combine(_appDomainProvider.GetAppDataFolderPath(), "users", userName);

    private string BuildPath(string userName)
        => Path.Combine(BuildUserFolderPath(userName), FileName);

    private async Task<byte[]> ReadFileContentAsync(string filePath)
    {
        try
        {
            await semaphore.WaitAsync();

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
            semaphore.Release();
        }
    }

    private async Task WriteFileContentAsync(string filePath, byte[] content)
    {
        await semaphore.WaitAsync();

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
            semaphore.Release();
        }
    }

    #endregion
}
