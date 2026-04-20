using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Persistence;
using System.Security.Cryptography;

namespace Pango.Infrastructure.Services;

/// <summary>
/// Handles encryption and decryption directly via streams to drastically reduce RAM consumption.
/// </summary>
public class ContentEncoder : IContentEncoder
{
    private static readonly JsonSerializer _serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    });

    /// <summary>
    /// Decrypts a byte array streaming directly to the JSON deserializer.
    /// </summary>
    public async Task<T?> DecryptAsync<T>(byte[] encryptedContent, string key, string salt)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(salt))
                    throw new ArgumentException("Key and Salt cannot be empty");

                byte[] keyBytes = Convert.FromBase64String(key);
                byte[] ivBytes = Convert.FromBase64String(salt);

                using Aes aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                using MemoryStream memoryStream = new(encryptedContent);
                using ICryptoTransform decryptor = aes.CreateDecryptor();
                using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
                using StreamReader streamReader = new(cryptoStream);
                using JsonTextReader jsonReader = new(streamReader);

                var deserializedObject = _serializer.Deserialize<T>(jsonReader);

                if (deserializedObject is IHaveEncodedData encodedData && encodedData.Data != null)
                {
                    if (encodedData.Data is JArray jArray)
                    {
                        var dataType = Type.GetType(encodedData.DataType ?? string.Empty);
                        if (dataType != null)
                        {
                            encodedData.Data = jArray.ToObject(dataType, _serializer);
                        }
                    }
                }

                return deserializedObject;
            }
            catch (Exception ex)
            {
                throw new PangoDataDecryptionException(
                    ApplicationErrors.Data.DecryptionError, $"Decryption failed for {typeof(T).Name}.", ex);
            }
        });
    }

    /// <summary>
    /// Encrypts an object by serializing it directly to the crypto stream.
    /// </summary>
    public async Task<byte[]> EncryptAsync<T>(T content, string key, string salt)
    {
        return await Task.Run(() =>
        {
            try
            {
                byte[] keyBytes = Convert.FromBase64String(key);
                byte[] ivBytes = Convert.FromBase64String(salt);

                using Aes aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                using MemoryStream memoryStream = new();
                using ICryptoTransform encryptor = aes.CreateEncryptor();
                using CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write);
                using StreamWriter streamWriter = new(cryptoStream);
                using JsonTextWriter jsonWriter = new(streamWriter);

                _serializer.Serialize(jsonWriter, content);
                jsonWriter.Flush();
                streamWriter.Flush();
                cryptoStream.FlushFinalBlock();

                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new PangoDataEncryptionException(ApplicationErrors.Data.EncryptionError, $"Cannot encrypt data of type {typeof(T).Name}", ex);
            }
        });
    }
}
