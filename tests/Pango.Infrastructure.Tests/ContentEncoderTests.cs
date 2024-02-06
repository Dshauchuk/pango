using Moq;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Services;
using Pango.Infrastructure.Services;

namespace Pango.Infrastructure.Tests;

//public class ContentEncoderTests
//{
//    public ContentEncoderTests()
//    {

//    }

//    [Fact]
//    public async Task Encode_Decode_Equal_Content()
//    {
//        // Arrange
//        var mockUserContextProvider = new Mock<IUserContextProvider>();
//        var encoder = new ContentEncoder(mockUserContextProvider.Object);
//        string expectedKey = "BDOkNgypl07TdasJlVfrZV6Wk3dxCsba1uLFBzY+Yjc=";
//        string expectedSalt = "cfSXpOhLueOiOKpkhXVm9g==";
//        string content = JsonConvert.SerializeObject(new List<Password>() { new Password() { Login = "me", Value = "my password", CreatedAt = DateTime.Now, Name = "my pwd" } });

//        mockUserContextProvider.Setup(provider => provider.GetKeyAsync()).ReturnsAsync(expectedKey);
//        mockUserContextProvider.Setup(provider => provider.GetSaltAsync()).ReturnsAsync(expectedSalt);

//        // Act
//        string encodedContent = await encoder.EncryptAsync(new List<Password>() { new Password() { Login = "me", Value = "my password", CreatedAt = DateTime.Now, Name = "my pwd" } });

//        var decoded = await encoder.DecryptAsync<List<Password>>(encodedContent);



//        //var encoded = ContentEncoder.Encrypt(content, Convert.FromBase64String(expectedKey), Convert.FromBase64String(expectedSalt));
//        //var decoded = ContentEncoder.Decrypt(encoded, Convert.FromBase64String(expectedKey), Convert.FromBase64String(expectedSalt));
 
//    }

//}

public class ContentEncoderTests
{
    [Fact]
    public async Task EncryptAsync_Returns_Encrypted_Content()
    {
        // Arrange
        var userContextProviderMock = new Mock<IUserContextProvider>();
        userContextProviderMock.SetupSequence(p => p.GetKeyAsync())
            .ReturnsAsync(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));

        var encoder = new ContentEncoder(userContextProviderMock.Object);
        var content = new { Message = "Hello, World!" };

        // Act
        var encryptedContent = await encoder.EncryptAsync(content);

        // Assert
        Assert.NotNull(encryptedContent);
        Assert.NotEmpty(encryptedContent);
    }

    [Fact]
    public async Task DecryptAsync_Returns_Decrypted_Content()
    {
        // Arrange
        var userContextProviderMock = new Mock<IUserContextProvider>();
        userContextProviderMock.SetupSequence(p => p.GetKeyAsync())
            .ReturnsAsync(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));

        var encoder = new ContentEncoder(userContextProviderMock.Object);
        var content = new { Message = "Hello, World!" };
        var encryptedContent = await encoder.EncryptAsync(content);

        // Act
        var decryptedContent = await encoder.DecryptAsync<dynamic>(encryptedContent);

        // Assert
        Assert.NotNull(decryptedContent);
        Assert.Equal(content.Message, decryptedContent.Message.ToString());
    }

    [Fact]
    public async Task EncryptAsync_Throws_Exception_When_Content_Null()
    {
        // Arrange
        var userContextProviderMock = new Mock<IUserContextProvider>();
        userContextProviderMock.SetupSequence(p => p.GetKeyAsync())
            .ReturnsAsync(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));

        var encoder = new ContentEncoder(userContextProviderMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DataEncryptionException>(() => encoder.EncryptAsync<object>(null));
    }

    [Fact]
    public async Task DecryptAsync_Throws_Exception_When_Content_Invalid()
    {
        // Arrange
        var userContextProviderMock = new Mock<IUserContextProvider>();
        userContextProviderMock.SetupSequence(p => p.GetKeyAsync())
            .ReturnsAsync(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));

        var encoder = new ContentEncoder(userContextProviderMock.Object);
        var encryptedContent = "InvalidContent";

        // Act & Assert
        await Assert.ThrowsAsync<DataEncryptionException>(() => encoder.DecryptAsync<dynamic>(encryptedContent));
    }
}
