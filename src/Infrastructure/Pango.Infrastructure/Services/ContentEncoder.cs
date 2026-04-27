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
        if (encryptedContent == null || encryptedContent.Length == 0) return default;

        return await Task.Run(() => DecryptInternal<T>(encryptedContent, key, salt));
    }

    private static T? DecryptInternal<T>(byte[] encryptedContent, string key, string salt)
    {
        try
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(salt))
                return default;

            if (encryptedContent.Length % AppConstants.Security.AesIvSize != 0)
                return default;

            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] ivBytes = Convert.FromBase64String(salt);

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            aes.Padding = PaddingMode.None;
            aes.Mode = CipherMode.CBC;

            byte[] decryptedBytes;
            using (var decryptor = aes.CreateDecryptor())
            {
                decryptedBytes = decryptor.TransformFinalBlock(encryptedContent, 0, encryptedContent.Length);
            }

            if (decryptedBytes.Length == 0) return default;

            int paddingLen = decryptedBytes[^1];
            if (paddingLen <= 0 || paddingLen > 16 || paddingLen > decryptedBytes.Length)
                return default;

            for (int i = 0; i < paddingLen; i++)
            {
                if (decryptedBytes[decryptedBytes.Length - 1 - i] != paddingLen)
                    return default;
            }

            int actualDataLen = decryptedBytes.Length - paddingLen;

            for (int i = 0; i < actualDataLen; i++)
            {
                byte b = decryptedBytes[i];
                if (b < 32 && b != 9 && b != 10 && b != 13)
                    return default;
            }

            using var memoryStream = new MemoryStream(decryptedBytes, 0, actualDataLen);
            using var streamReader = new StreamReader(memoryStream);
            using var jsonReader = new JsonTextReader(streamReader);

            var deserializedObject = _serializer.Deserialize<T>(jsonReader);

            if (deserializedObject is IHaveEncodedData encodedData && encodedData.Data != null)
            {
                if (encodedData.Data is JArray jArray && !string.IsNullOrEmpty(encodedData.DataType))
                {
                    var dataType = Type.GetType(encodedData.DataType) ??
                                   (encodedData.DataType.Contains("PangoPassword") ? typeof(List<Domain.Entities.PangoPassword>) : null);

                    if (dataType != null)
                        encodedData.Data = jArray.ToObject(dataType, _serializer);
                }
            }
            return deserializedObject;
        }
        catch
        {
            return default;
        }
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

                using MemoryStream memoryStream = new(AppConstants.Security.MemoryStreamDefaultCapacity);
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
