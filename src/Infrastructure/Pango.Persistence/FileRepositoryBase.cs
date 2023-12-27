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
        string encryptedFileContent = await ReadFileContentAsync(filePath);

        return await _contentEncoder.DecryptAsync<IEnumerable<T>>(encryptedFileContent) ?? Enumerable.Empty<T>();
    }

    protected async Task SaveItemsForUserAsync(string userId, IEnumerable<T> items)
    {
        string filePath = BuildPath(userId);
        string content = await _contentEncoder.EncryptAsync(items);

        await WriteFileContent(filePath, content);
    }

    #region Private Methods

    private string BuildPath(string userId)
        => Path.Combine(_appDomainProvider.GetAppDataFolderPath(), "users", userId, FileName);

    private async Task<string> ReadFileContentAsync(string filePath)
    {
        if(!File.Exists(filePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.Create(filePath).Dispose();
        }

        using var reader = File.OpenText(filePath);
        string content = await reader.ReadToEndAsync();

        return content;
    }

    private Task WriteFileContent(string filePath, string content)
    {
        return Task.Run(() => File.WriteAllText(filePath, content));
    }

    #endregion
}
