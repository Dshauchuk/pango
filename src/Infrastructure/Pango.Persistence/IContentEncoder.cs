namespace Pango.Persistence;

public interface IContentEncoder
{
    Task<byte[]> EncryptAsync<T>(T content, string key, string salt);

    Task<T?> DecryptAsync<T>(byte[] encryptedContent, string key, string salt);
}
