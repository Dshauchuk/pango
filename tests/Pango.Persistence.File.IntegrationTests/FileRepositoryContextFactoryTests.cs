using Moq;
using Pango.Application.Common;

namespace Pango.Persistence.File.IntegrationTests;

public class FileRepositoryContextFactoryTests
{
    [Fact]
    public void Create_ReturnsValidContext()
    {
        // Arrange
        var domainMock = new Mock<IAppDomainProvider>();
        domainMock.Setup(x => x.GetUserFolderPath("testuser")).Returns("C:\\temp\\testuser");

        var factory = new FileRepositoryContextFactory(domainMock.Object);
        var encoding = new EncodingOptions("key", "salt");

        // Act
        var context = factory.Create("testuser", encoding);

        // Assert
        Assert.IsType<FileRepositoryActionContext>(context);
        var fileContext = (FileRepositoryActionContext)context;

        Assert.Equal("testuser", fileContext.UserId);
        Assert.Equal("C:\\temp\\testuser", fileContext.WorkingDirectoryPath);
        Assert.Equal("key", fileContext.EncodingOptions.Key);
        Assert.Equal("salt", fileContext.EncodingOptions.Salt);
    }
}
