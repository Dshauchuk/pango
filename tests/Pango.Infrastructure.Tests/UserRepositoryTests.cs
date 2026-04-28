using Moq;
using Pango.Application.Common;
using Pango.Domain.Entities;
using Pango.Persistence;

namespace Pango.Infrastructure.Tests;

public class UserRepositoryTests
{
    private readonly Mock<IPasswordVault> _vaultMock = new();
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _repository = new UserRepository(_vaultMock.Object);
    }

    // A simple stub for ICredentials to return from the mock
    private class TestCredentials : ICredentials
    {
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
    }

    [Fact]
    public async Task CreateAsync_CallsVaultAddAsync()
    {
        // Arrange
        var user = new PangoUser { UserName = "alice", MasterPasswordHash = "hash", PasswordSalt = "salt" };

        // Act
        await _repository.CreateAsync(user);

        // Assert
        _vaultMock.Verify(v => v.AddAsync(
            "Pango.Desktop.Uwp.Users",
            "alice",
            "hash",
            It.Is<IDictionary<string, object>>(d => d.ContainsKey(UserProperties.PasswordSalt) && d[UserProperties.PasswordSalt].ToString() == "salt")),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CallsVaultRemoveAsync()
    {
        // Arrange
        var user = new PangoUser { UserName = "bob" };

        // Act
        await _repository.DeleteAsync(user);

        // Assert
        _vaultMock.Verify(v => v.RemoveAsync("Pango.Desktop.Uwp.Users", "bob"), Times.Once);
    }

    [Fact]
    public async Task FindAsync_ReturnsNull_WhenVaultReturnsNull()
    {
        // Arrange
        _vaultMock.Setup(v => v.FindAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((ICredentials?)null);

        // Act
        var result = await _repository.FindAsync("ghost");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindAsync_ReturnsUser_WhenCredentialsExist()
    {
        // Arrange
        _vaultMock.Setup(v => v.FindAsync("Pango.Desktop.Uwp.Users", "alice"))
            .ReturnsAsync(new TestCredentials { UserName = "alice", PasswordHash = "h", PasswordSalt = "s" });

        // Act
        var result = await _repository.FindAsync("alice");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("alice", result!.UserName);
        Assert.Equal("h", result.MasterPasswordHash);
        Assert.Equal("s", result.PasswordSalt);
    }

    [Fact]
    public async Task ListAsync_ReturnsUsers_MappedFromVault()
    {
        // Arrange
        _vaultMock.Setup(v => v.ListUsersAsync("Pango.Desktop.Uwp.Users"))
            .ReturnsAsync(["alice", "bob"]);

        // Act
        var results = (await _repository.ListAsync()).ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, u => u.UserName == "alice");
        Assert.Contains(results, u => u.UserName == "bob");
    }
}
