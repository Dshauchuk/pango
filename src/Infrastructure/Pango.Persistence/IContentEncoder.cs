namespace Pango.Persistence;

public interface IContentEncoder
{
    Task<byte[]> EncryptAsync<T>(T content);

    Task<T?> DecryptAsync<T>(byte[] encryptedContent);
}
