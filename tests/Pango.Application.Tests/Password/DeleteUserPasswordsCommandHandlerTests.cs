using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Password.Commands.DeleteUserPasswords;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.Password;

public class DeleteUserPasswordsCommandHandlerTests
{
    private readonly Mock<IPasswordRepository> _passwords = new();
    private readonly Mock<IUserContextProvider> _ctx = new();
    private readonly Mock<IRepositoryContextFactory> _factory = new();
    private readonly Mock<IRepositoryActionContext> _action = new();
    private readonly Mock<ILogger<DeleteUserPasswordsCommandHandler>> _logger = new();

    public DeleteUserPasswordsCommandHandlerTests()
    {
        _ctx.Setup(x => x.GetUserName()).Returns("u");
        _ctx.Setup(x => x.GetEncodingOptionsAsync()).ReturnsAsync(new EncodingOptions("k", "s"));
        _factory.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<EncodingOptions>())).Returns(_action.Object);
    }

    [Fact]
    public async Task Handle_ReturnsFalse_WhenNoPasswords()
    {
        _passwords.Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync(Array.Empty<PangoPassword>());

        var handler = new DeleteUserPasswordsCommandHandler(_passwords.Object, _ctx.Object, _logger.Object, _factory.Object);
        var result = await handler.Handle(new DeleteUserPasswordsCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task Handle_DeletesAll_WhenPasswordsExist()
    {
        var a = new PangoPassword { Id = Guid.NewGuid() };
        var b = new PangoPassword { Id = Guid.NewGuid() };
        _passwords.Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync(new[] { a, b });

        var handler = new DeleteUserPasswordsCommandHandler(_passwords.Object, _ctx.Object, _logger.Object, _factory.Object);
        var result = await handler.Handle(new DeleteUserPasswordsCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.True(result.Value);
        _passwords.Verify(x => x.DeleteAsync(a, _action.Object), Times.Once);
        _passwords.Verify(x => x.DeleteAsync(b, _action.Object), Times.Once);
    }
}
