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
}
