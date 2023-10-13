namespace Pango.Persistence;

public interface IContentEncoder
{
    Task<string> EncryptAsync<T>(T content);

    Task<T?> DecryptAsync<T>(string encryptedContent);
}
