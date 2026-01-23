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
    public Task<T?> DecryptAsync<T>(byte[] encryptedContent, string key, string salt)
    {
        try
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(salt))
                throw new ArgumentException("Key and Salt cannot be empty");

            byte[] keyBytes;
            byte[] ivBytes;

            try
            {
                keyBytes = Convert.FromBase64String(key);
                ivBytes = Convert.FromBase64String(salt);
            }
            catch (FormatException ex)
            {
                throw new PangoDataDecryptionException(ApplicationErrors.Data.DecryptionError, "Invalid key format (Base64 expected).", ex);
            }

            string jsonContent = Decrypt(encryptedContent, keyBytes, ivBytes);

            var deserializedObject = JsonConvert.DeserializeObject<T>(jsonContent);

            // DS
            // this code converts JArray into a List<object>
            if (deserializedObject is IHaveEncodedData encodedData && encodedData.Data != null)
            {
                var encodedDataType = encodedData.Data.GetType();
                if (encodedDataType == typeof(JArray))
                {
                    var dataType = Type.GetType(encodedData.DataType ?? string.Empty);
                    if (dataType != null)
                    {
                        encodedData.Data = ((JArray)encodedData.Data).ToObject(dataType);
                    }
                }
            }

            return Task.FromResult(deserializedObject);
        }
        catch (PangoDataDecryptionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new PangoDataDecryptionException(
                ApplicationErrors.Data.DecryptionError,
                $"Decryption failed for {typeof(T).Name}.",
                ex);
        }
    }

    public Task<byte[]> EncryptAsync<T>(T content, string key, string salt)
    {
        try
        {
            string json = JsonConvert.SerializeObject(content, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

            byte[] result = Encrypt(json, Convert.FromBase64String(key), Convert.FromBase64String(salt));
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            throw new PangoDataEncryptionException(ApplicationErrors.Data.EncryptionError, $"Cannot encrypt data of type {typeof(T).Name}", ex);
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
            using MemoryStream memoryStream = new();
            using CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write);
            using (StreamWriter streamWriter = new(cryptoStream))
            {
                streamWriter.Write(simpletext);
            }

            cipheredtext = memoryStream.ToArray();
        }
        return cipheredtext;
    }

    private static string Decrypt(byte[] cipheredText, byte[] key, byte[] iv)
    {
        Ensure.AreEqual(key.Length, 32, nameof(key));
        Ensure.AreEqual(iv.Length, 16, nameof(iv));

        try
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            using ICryptoTransform decryptor = aes.CreateDecryptor();
            using MemoryStream memoryStream = new(cipheredText);
            using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
            using StreamReader streamReader = new(cryptoStream);

            return streamReader.ReadToEnd();
        }
        catch (CryptographicException ex)
        {
            throw new PangoDataDecryptionException(ApplicationErrors.Data.DecryptionError, "Data decryption failed: probably the key is wrong", ex);
        }
    }
}
