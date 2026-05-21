using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.UseCases.User.Commands.Register;
using Pango.Application.Tests.TestDoubles;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.User;

public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ILogger<RegisterUserCommandHandler>> _logger = new();

    [Fact]
    public async Task Handle_CreatesUser_WhenUnderLimit()
    {
        _users.Setup(x => x.ListAsync()).ReturnsAsync(new List<PangoUser>());
        var hasher = new FakePasswordHashProvider();

        var handler = new RegisterUserCommandHandler(_users.Object, hasher, _logger.Object);
        var result = await handler.Handle(new RegisterUserCommand("newuser", "secret"), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("newuser", result.Value.UserName);
        _users.Verify(x => x.CreateAsync(It.Is<PangoUser>(u => u.UserName == "newuser" && u.MasterPasswordHash == "hashed-master")), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsValidation_WhenMaxUsersReached()
    {
        var many = Enumerable.Range(0, 5).Select(i => new PangoUser { UserName = $"u{i}" }).ToList();
        _users.Setup(x => x.ListAsync()).ReturnsAsync(many);

        var handler = new RegisterUserCommandHandler(_users.Object, new FakePasswordHashProvider(), _logger.Object);
        var result = await handler.Handle(new RegisterUserCommand("sixth", "pw"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.User.TooManyUsers, result.FirstError.Code);
        _users.Verify(x => x.CreateAsync(It.IsAny<PangoUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenCreateThrows()
    {
        _users.Setup(x => x.ListAsync()).ReturnsAsync(Array.Empty<PangoUser>());
        _users.Setup(x => x.CreateAsync(It.IsAny<PangoUser>())).ThrowsAsync(new IOException("disk full"));

        var handler = new RegisterUserCommandHandler(_users.Object, new FakePasswordHashProvider(), _logger.Object);
        var result = await handler.Handle(new RegisterUserCommand("u", "p"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.User.RegistrationFailed, result.FirstError.Code);
    }
}
