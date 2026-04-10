using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.UseCases.User.Commands.Delete;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.User;

public class DeleteUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserStorageManager> _storage = new();
    private readonly Mock<ILogger<DeleteUserCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_ReturnsFailure_WhenUserMissing()
    {
        _users.Setup(x => x.FindAsync("ghost")).ReturnsAsync((PangoUser?)null);
        var handler = new DeleteUserCommandHandler(_users.Object, _storage.Object, _logger.Object);

        var result = await handler.Handle(new DeleteUserCommand("ghost"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("user_not_found", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_DeletesUserAndData_WhenFound()
    {
        var user = new PangoUser { UserName = "alice" };
        _users.Setup(x => x.FindAsync("alice")).ReturnsAsync(user);

        var handler = new DeleteUserCommandHandler(_users.Object, _storage.Object, _logger.Object);
        var result = await handler.Handle(new DeleteUserCommand("alice"), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.True(result.Value);
        _users.Verify(x => x.DeleteAsync(user), Times.Once);
        _storage.Verify(x => x.DeleteAllUserDataAsync("alice"), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenDeleteThrows()
    {
        var user = new PangoUser { UserName = "bob" };
        _users.Setup(x => x.FindAsync("bob")).ReturnsAsync(user);
        _users.Setup(x => x.DeleteAsync(user)).ThrowsAsync(new UnauthorizedAccessException());

        var handler = new DeleteUserCommandHandler(_users.Object, _storage.Object, _logger.Object);
        var result = await handler.Handle(new DeleteUserCommand("bob"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.User.DeletionFailed, result.FirstError.Code);
    }
}
