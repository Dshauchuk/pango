namespace Pango.Persistence;

public abstract class FileRepositoryBase<T>
{
    private readonly IContentEncoder _contentEncoder;
    private readonly IAppDomainProvider _appDomainProvider;

    public FileRepositoryBase(IContentEncoder contentEncoder, IAppDomainProvider appDomainProvider)
    {
        _contentEncoder = contentEncoder;
        _appDomainProvider = appDomainProvider;
    }

    protected abstract string FileName { get; }

    protected async Task<IEnumerable<T>> ExtractAllItemsForUserAsync(string userId)
    {
        string filePath = BuildPath(userId);
        byte[] encryptedFileContent = await ReadFileContentAsync(filePath);

        return await _contentEncoder.DecryptAsync<IEnumerable<T>>(encryptedFileContent) ?? Enumerable.Empty<T>();
    }

    protected async Task SaveItemsForUserAsync(string userId, IEnumerable<T> items)
    {
        string filePath = BuildPath(userId);
        byte[] content = await _contentEncoder.EncryptAsync(items);

        await WriteFileContentAsync(filePath, content);
    }

    #region Private Methods

    private string BuildPath(string userId)
        => Path.Combine(_appDomainProvider.GetAppDataFolderPath(), "users", userId, FileName);

    private async Task<byte[]> ReadFileContentAsync(string filePath)
    {
        if(!File.Exists(filePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.Create(filePath).Dispose();
        }

        byte[] result;
        using (FileStream stream = File.Open(filePath, FileMode.Open))
        {
            result = new byte[stream.Length];
            await stream.ReadAsync(result, 0, (int)stream.Length);
        }

        return result;
    }

    private async Task WriteFileContentAsync(string filePath, byte[] content)
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

    #endregion
}
