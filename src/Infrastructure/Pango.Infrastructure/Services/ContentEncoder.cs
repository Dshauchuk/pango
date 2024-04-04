﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Services;
using Pango.Persistence;
using System.Security.Cryptography;

namespace Pango.Infrastructure.Services;
public class MyObjectConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(object);
    }

    public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case Newtonsoft.Json.JsonToken.StartArray:
                return JToken.Load(reader).ToObject<List<object>>();
            case Newtonsoft.Json.JsonToken.StartObject:
                return JToken.Load(reader).ToObject<Dictionary<string, object>>();
            default:
                if (reader.ValueType == null && reader.TokenType != Newtonsoft.Json.JsonToken.Null)
                    throw new NotImplementedException("MyObjectConverter");
                return reader.Value;
        }
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotSupportedException("MyObjectConverter");
    }
}

public class ContentEncoder : IContentEncoder
{
    private readonly IUserContextProvider _userContextProvider;

    public ContentEncoder(IUserContextProvider userContextProvider)
    {
        _userContextProvider = userContextProvider;
    }

    public async Task<T?> DecryptAsync<T>(byte[] encryptedContent)
    {
        try
        {
            string jsonContent = Decrypt(encryptedContent, await GetKeyAsync(), await GetVectorAsync());

            return JsonConvert.DeserializeObject<T>(jsonContent, new MyObjectConverter());
        }
        catch(Exception ex)
        {
            throw new DataEncryptionException(ApplicationErrors.Data.EncryptionError, $"Cannot decrypt data of type {typeof(T).Name}", ex);
        }
    }

    public async Task<byte[]> EncryptAsync<T>(T content)
    {
        try
        {
            string json = JsonConvert.SerializeObject(content);

            return Encrypt(json, await GetKeyAsync(), await GetVectorAsync());
        }
        catch (Exception ex)
        {
            throw new DataEncryptionException(ApplicationErrors.Data.EncryptionError, $"Cannot encrypt data of type {typeof(T).Name}", ex);
        }
    }

    private async Task<byte[]> GetKeyAsync()
    {
        return Convert.FromBase64String(await _userContextProvider.GetKeyAsync());
    }

    private async Task<byte[]> GetVectorAsync()
    {
        return Convert.FromBase64String(await _userContextProvider.GetSaltAsync());
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
