using Moq;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;
using Pango.Infrastructure.Services;
using Pango.Persistence;

namespace Pango.Infrastructure.Tests;

public class UserContextProviderTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IAppUserProvider> _appUserProviderMock = new();

    [Fact]
    public void GetUserName_ThrowsUnauthorizedException_WhenUserIsEmpty()
    {
        // Arrange
        _appUserProviderMock.Setup(x => x.GetUserId()).Returns(string.Empty);
        var provider = new UserContextProvider(_userRepoMock.Object, _appUserProviderMock.Object);

        // Act & Assert
        Assert.Throws<UnauthorizedException>(() => provider.GetUserName());
    }

    [Fact]
    public void GetUserName_ReturnsUserId_WhenUserExists()
    {
        // Arrange
        _appUserProviderMock.Setup(x => x.GetUserId()).Returns("testuser");
        var provider = new UserContextProvider(_userRepoMock.Object, _appUserProviderMock.Object);

        // Act
        var result = provider.GetUserName();

        // Assert
        Assert.Equal("testuser", result);
    }

    [Fact]
    public async Task GetEncodingOptionsAsync_ThrowsUnauthorizedException_WhenUserNotFoundInDb()
    {
        // Arrange
        _appUserProviderMock.Setup(x => x.GetUserId()).Returns("testuser");
        _userRepoMock.Setup(x => x.FindAsync("testuser")).ReturnsAsync((PangoUser?)null);
        var provider = new UserContextProvider(_userRepoMock.Object, _appUserProviderMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => provider.GetEncodingOptionsAsync());
    }

    [Fact]
    public async Task GetEncodingOptionsAsync_ReturnsOptions_WhenUserFound()
    {
        // Arrange
        _appUserProviderMock.Setup(x => x.GetUserId()).Returns("testuser");
        _userRepoMock.Setup(x => x.FindAsync("testuser")).ReturnsAsync(new PangoUser
        {
            UserName = "testuser",
            MasterPasswordHash = "hash",
            PasswordSalt = "salt"
        });

        var provider = new UserContextProvider(_userRepoMock.Object, _appUserProviderMock.Object);

        // Act
        var options = await provider.GetEncodingOptionsAsync();

        // Assert
        Assert.Equal("hash", options.Key);
        Assert.Equal("salt", options.Salt);

        var key = await provider.GetKeyAsync();
        var salt = await provider.GetSaltAsync();
        Assert.Equal("hash", key);
        Assert.Equal("salt", salt);
    }
}
