using Pango.Application.Common.Exceptions;
using Pango.Infrastructure.Services;
using System.Security.Cryptography;

namespace Pango.Infrastructure.Tests;

public class ContentEncoderTests
{
    private readonly ContentEncoder _encoder = new();

    private static (string key, string salt) GenerateValidKeys()
    {
        var keyBytes = new byte[32];
        var ivBytes = new byte[16];
        RandomNumberGenerator.Fill(keyBytes);
        RandomNumberGenerator.Fill(ivBytes);

        return (Convert.ToBase64String(keyBytes), Convert.ToBase64String(ivBytes));
    }

    [Fact]
    public async Task Encrypt_And_Decrypt_String_Successfully()
    {
        // Arrange
        var (key, salt) = GenerateValidKeys();
        string originalContent = "Secret Message 123";

        // Act
        byte[] encryptedBytes = await _encoder.EncryptAsync(originalContent, key, salt);
        string? decryptedContent = await _encoder.DecryptAsync<string>(encryptedBytes, key, salt);

        // Assert
        Assert.NotNull(encryptedBytes);
        Assert.NotEmpty(encryptedBytes);
        Assert.Equal(originalContent, decryptedContent);
    }

    [Fact]
    public async Task Encrypt_And_Decrypt_ComplexObject_Successfully()
    {
        // Arrange
        var (key, salt) = GenerateValidKeys();
        var originalObj = new TestData { Id = 1, Name = "Test" };

        // Act
        byte[] encryptedBytes = await _encoder.EncryptAsync(originalObj, key, salt);
        var decryptedObj = await _encoder.DecryptAsync<TestData>(encryptedBytes, key, salt);

        // Assert
        Assert.NotNull(decryptedObj);
        Assert.Equal(originalObj.Id, decryptedObj.Id);
        Assert.Equal(originalObj.Name, decryptedObj.Name);
    }

    [Fact]
    public async Task Decrypt_ReturnsDefault_WhenContentEmptyOrNull()
    {
        var (key, salt) = GenerateValidKeys();

        var result1 = await _encoder.DecryptAsync<string>([], key, salt);
        var result2 = await _encoder.DecryptAsync<string>(null!, key, salt);

        Assert.Null(result1);
        Assert.Null(result2);
    }

    [Fact]
    public async Task Encrypt_ThrowsPangoDataEncryptionException_OnInvalidKey()
    {
        // Arrange
        string invalidKey = "invalid-base64";

        // Act & Assert
        await Assert.ThrowsAsync<PangoDataEncryptionException>(() =>
            _encoder.EncryptAsync("data", invalidKey, "salt"));
    }

    public class TestData
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
