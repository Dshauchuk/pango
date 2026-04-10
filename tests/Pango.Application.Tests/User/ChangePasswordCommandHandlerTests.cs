using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.User.Commands.ChangePassword;
using Pango.Application.Tests.TestDoubles;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.User;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IUserStorageManager> _storage = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ILogger<ChangePasswordCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_UpdatesStorageAndCredentials_WhenUserExists()
    {
        var user = new PangoUser { UserName = "alice" };
        _users.Setup(x => x.FindAsync("alice")).ReturnsAsync(user);
        _users.Setup(x => x.DeleteAsync(user)).Returns(Task.CompletedTask);
        _users.Setup(x => x.CreateAsync(It.IsAny<PangoUser>())).Returns(Task.CompletedTask);
        _storage.Setup(x => x.EncryptDataWithAsync("alice", It.IsAny<EncodingOptions>())).Returns(Task.CompletedTask);

        var handler = (IRequestHandler<ChangePasswordCommand, ErrorOr<bool>>)new ChangePasswordCommandHandler(_storage.Object, new FakePasswordHashProvider(), _users.Object, _logger.Object);
        var result = await handler.Handle(new ChangePasswordCommand("alice", "newpw", "ignored"), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.True(result.Value);
        _storage.Verify(x => x.EncryptDataWithAsync("alice", It.IsAny<EncodingOptions>()), Times.Once);
        _users.Verify(x => x.CreateAsync(It.Is<PangoUser>(u => u.MasterPasswordHash == "hashed-master")), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenUserNotFound()
    {
        _users.Setup(x => x.FindAsync("missing")).ReturnsAsync((PangoUser?)null);

        var handler = (IRequestHandler<ChangePasswordCommand, ErrorOr<bool>>)new ChangePasswordCommandHandler(_storage.Object, new FakePasswordHashProvider(), _users.Object, _logger.Object);
        var result = await handler.Handle(new ChangePasswordCommand("missing", "pw", "s"), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.False(result.Value);
    }
}
