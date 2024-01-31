using Newtonsoft.Json;
using Pango.Persistence;
using System.Security.Cryptography;

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

    private static byte[] Encrypt(string simpletext, byte[] key, byte[] iv)
    {
        byte[] cipheredtext;
        using (Aes aes = Aes.Create())
        {
            ICryptoTransform encryptor = aes.CreateEncryptor(key, iv);
            using (MemoryStream memoryStream = new())
            {
                using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter streamWriter = new(cryptoStream))
                    {
                        streamWriter.Write(simpletext);
                    }

                    cipheredtext = memoryStream.ToArray();
                }
            }
        }
        return cipheredtext;
    }

    private static string Decrypt(byte[] cipheredText, byte[] key, byte[] iv)
    {
        string simpletext = String.Empty;
        using (Aes aes = Aes.Create())
        {
            ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
            using (MemoryStream memoryStream = new(cipheredText))
            {
                using (CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader streamReader = new(cryptoStream))
                    {
                        simpletext = streamReader.ReadToEnd();
                    }
                }
            }
        }
        return simpletext;
    }
}
