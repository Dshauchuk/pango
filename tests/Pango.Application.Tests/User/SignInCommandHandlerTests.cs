using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.User.Commands.SignIn;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.User;

public class SignInCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHashProvider> _hasher = new();
    private readonly Mock<ILogger<SignInCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_ReturnsFalse_WhenUserNotFound()
    {
        _users.Setup(x => x.FindAsync("nouser")).ReturnsAsync((PangoUser?)null);
        var handler = new SignInCommandHandler(_users.Object, _hasher.Object, _logger.Object);

        var result = await handler.Handle(new SignInCommand("nouser", "pw"), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.False(result.Value);
        _hasher.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsVerificationResult_WhenUserExists()
    {
        var user = new PangoUser { UserName = "alice", MasterPasswordHash = "hash", PasswordSalt = Convert.ToBase64String(new byte[] { 1, 2, 3 }) };
        _users.Setup(x => x.FindAsync("alice")).ReturnsAsync(user);
        _hasher.Setup(x => x.VerifyPassword("pw", "hash", It.IsAny<byte[]>())).Returns(true);

        var handler = new SignInCommandHandler(_users.Object, _hasher.Object, _logger.Object);
        var result = await handler.Handle(new SignInCommand("alice", "pw"), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenRepositoryThrows()
    {
        _users.Setup(x => x.FindAsync("bob")).ThrowsAsync(new InvalidOperationException("db"));

        var handler = new SignInCommandHandler(_users.Object, _hasher.Object, _logger.Object);
        var result = await handler.Handle(new SignInCommand("bob", "pw"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.User.LoginFailed, result.FirstError.Code);
    }
}
