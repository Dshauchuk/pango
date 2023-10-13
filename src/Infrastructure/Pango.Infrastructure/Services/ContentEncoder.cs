using Newtonsoft.Json;
using Pango.Persistence;

namespace Pango.Infrastructure.Services;

public class ContentEncoder : IContentEncoder
{
    public Task<T?> DecryptAsync<T>(string encryptedContent)
    {
        return Task.Run(() => JsonConvert.DeserializeObject<T>(encryptedContent));
    }

    public Task<string> EncryptAsync<T>(T content)
    {
        return Task.Run(() => JsonConvert.SerializeObject(content));
    }
}
