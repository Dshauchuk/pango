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
    /// <summary>
    /// Decrypts a byte array into an object of type T using the provided key and salt.
    /// </summary>
    /// <param name="encryptedContent">The raw encrypted bytes.</param>
    /// <param name="key">The Base64 encoded encryption key.</param>
    /// <param name="salt">The Base64 encoded initialization vector (IV).</param>
    public async Task<T?> DecryptAsync<T>(byte[] encryptedContent, string key, string salt)
    {
        return await Task.Run(() =>
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

                return deserializedObject;
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
        });
    }

    /// <summary>
    /// Encrypts an object of type T into a byte array using the provided key and salt.
    /// </summary>
    /// <param name="content">The object to be encrypted.</param>
    /// <param name="key">The Base64 encoded encryption key.</param>
    /// <param name="salt">The Base64 encoded initialization vector (IV).</param>
    public async Task<byte[]> EncryptAsync<T>(T content, string key, string salt)
    {
        return await Task.Run(() =>
        {
            try
            {
                string json = JsonConvert.SerializeObject(content, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

                return Encrypt(json, Convert.FromBase64String(key), Convert.FromBase64String(salt));
            }
            catch (Exception ex)
            {
                throw new PangoDataEncryptionException(ApplicationErrors.Data.EncryptionError, $"Cannot encrypt data of type {typeof(T).Name}", ex);
            }
        });
    }

    /// <summary>
    /// Internal helper to execute AES encryption logic.
    /// </summary>
    private static byte[] Encrypt(string simpletext, byte[] key, byte[] iv)
    {
        Ensure.AreEqual(key.Length, 32, nameof(key));
        Ensure.AreEqual(iv.Length, 16, nameof(key));

        using Aes aes = Aes.Create();
        aes.Padding = PaddingMode.PKCS7;
        aes.Mode = CipherMode.CBC;

        using ICryptoTransform encryptor = aes.CreateEncryptor(key, iv);
        using MemoryStream memoryStream = new();
        using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write))
        using (StreamWriter streamWriter = new(cryptoStream))
        {
            streamWriter.Write(simpletext);
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Internal helper to execute AES decryption logic.
    /// </summary>
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
