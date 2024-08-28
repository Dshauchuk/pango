using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Persistence;
using System.Security.Cryptography;

namespace Pango.Infrastructure.Services;

public class ContentEncoder : IContentEncoder
{
    public async Task<T?> DecryptAsync<T>(byte[] encryptedContent, string key, string salt)
    {
        try
        {
            string jsonContent = await Task.Run(() => Decrypt(encryptedContent, Convert.FromBase64String(key), Convert.FromBase64String(salt)));

            var deserializedObject = JsonConvert.DeserializeObject<T>(jsonContent);

            // DS
            // this code converts JArray into a List<object>
            if(deserializedObject is IHaveEncodedData encodedData && encodedData.Data != null)
            {
                var encodedDataType = encodedData.Data.GetType();

                if (encodedDataType == typeof(JArray))
                {
                    var dataType = Type.GetType(encodedData.DataType ?? string.Empty);

                    if(dataType != null)
                    {
                        encodedData.Data = ((JArray)encodedData.Data).ToObject(dataType);
                    }
                }
            }

            return deserializedObject;
        }
        catch(Exception ex)
        {
            throw new DataEncryptionException(ApplicationErrors.Data.EncryptionError, $"Cannot decrypt data of type {typeof(T).Name}", ex);
        }
    }

    public async Task<byte[]> EncryptAsync<T>(T content, string key, string salt)
    {
        try
        {
            string json = JsonConvert.SerializeObject(content, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

            return await Task.Run(() => Encrypt(json, Convert.FromBase64String(key), Convert.FromBase64String(salt)));
        }
        catch (Exception ex)
        {
            throw new DataEncryptionException(ApplicationErrors.Data.EncryptionError, $"Cannot encrypt data of type {typeof(T).Name}", ex);
        }
    }

    private static byte[] Encrypt(string simpletext, byte[] key, byte[] iv)
    {
        Ensure.AreEqual(key.Length, 32, nameof(key));
        Ensure.AreEqual(iv.Length, 16, nameof(key));

        byte[] cipheredtext;
        using (Aes aes = Aes.Create())
        {
            aes.Padding = PaddingMode.PKCS7;

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
        Ensure.AreEqual(key.Length, 32, nameof(key));
        Ensure.AreEqual(iv.Length, 16, nameof(key));

        string simpletext = String.Empty;
        using (Aes aes = Aes.Create())
        {
            aes.Padding = PaddingMode.PKCS7;

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
