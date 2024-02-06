﻿using Newtonsoft.Json;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Services;
using Pango.Persistence;
using System.Security.Cryptography;

namespace Pango.Infrastructure.Services;

public class ContentEncoder : IContentEncoder
{
    private readonly IUserContextProvider _userContextProvider;

    public ContentEncoder(IUserContextProvider userContextProvider)
    {
        _userContextProvider = userContextProvider;
    }

    public async Task<T?> DecryptAsync<T>(string encryptedContent)
    {
        try
        {
            string jsonContent = Decrypt(Convert.FromBase64String(encryptedContent), await GetKeyAsync(), await GetVectorAsync());

            return JsonConvert.DeserializeObject<T>(jsonContent);
        }
        catch(Exception ex)
        {
            throw new DataEncryptionException(ApplicationErrors.Data.EncryptionError, $"Cannot decrypt data of type {typeof(T).Name}", ex);
        }
    }

    public async Task<string> EncryptAsync<T>(T content)
    {
        try
        {
            string json = JsonConvert.SerializeObject(content);

            return Convert.ToBase64String(Encrypt(json, await GetKeyAsync(), await GetVectorAsync()));
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
