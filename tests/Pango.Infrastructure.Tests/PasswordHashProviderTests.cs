using Pango.Infrastructure.Services;

namespace Pango.Infrastructure.Tests;

public class PasswordHashProviderTests
{
    private readonly PasswordHashProvider _passwordHashProvider;

    public PasswordHashProviderTests()
    {
        _passwordHashProvider = new PasswordHashProvider();
    }

    [Fact]
    public void Hash_Throws_ArgumentNullException_For_Null_Password()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _passwordHashProvider.Hash(null, out byte[] salt));
    }

    [Fact]
    public void Hash_Returns_Non_Null_Hash_And_Non_Null_Salt()
    {
        // Arrange
        string password = "myPassword";

        // Act
        string hash = _passwordHashProvider.Hash(password, out byte[] salt);

        // Assert
        Assert.NotNull(hash);
        Assert.NotNull(salt);
    }

    [Fact]
    public void VerifyPassword_Returns_False_For_Null_Hash()
    {
        // Act
        bool isValid = _passwordHashProvider.VerifyPassword("password", null, new byte[16]);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyPassword_Returns_False_For_Null_Salt()
    {
        // Act
        bool isValid = _passwordHashProvider.VerifyPassword("password", "hash", null);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyPassword_Returns_False_For_Empty_Salt()
    {
        // Act
        bool isValid = _passwordHashProvider.VerifyPassword("password", "hash", Array.Empty<byte>());

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyPassword_Returns_True_For_Valid_Password_And_Hash()
    {
        // Arrange
        string password = "myPassword";
        string hash = _passwordHashProvider.Hash(password, out byte[] salt);

        // Act
        bool isValid = _passwordHashProvider.VerifyPassword(password, hash, salt);

        // Assert
        Assert.True(isValid);
    }
}