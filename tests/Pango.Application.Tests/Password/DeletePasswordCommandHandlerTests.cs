using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Password.Commands.DeletePassword;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.Password;

public class DeletePasswordCommandHandlerTests
{
    private readonly Mock<IPasswordRepository> _passwords = new();
    private readonly Mock<IUserContextProvider> _ctx = new();
    private readonly Mock<IRepositoryContextFactory> _factory = new();
    private readonly Mock<IRepositoryActionContext> _action = new();
    private readonly Mock<ILogger<DeletePasswordCommandHandler>> _logger = new();

    public DeletePasswordCommandHandlerTests()
    {
        _ctx.Setup(x => x.GetUserName()).Returns("u");
        _ctx.Setup(x => x.GetEncodingOptionsAsync()).ReturnsAsync(new EncodingOptions("k", "s"));
        _factory.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<EncodingOptions>())).Returns(_action.Object);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenMissing()
    {
        _passwords.Setup(x => x.FindAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync((PangoPassword?)null);

        var handler = new DeletePasswordCommandHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var result = await handler.Handle(new DeletePasswordCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Password.NotFound, result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_DeletesSingleEntry_WhenNotCatalog()
    {
        var id = Guid.NewGuid();
        var pwd = new PangoPassword { Id = id, IsCatalog = false };
        _passwords.Setup(x => x.FindAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync(pwd);

        var handler = new DeletePasswordCommandHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var result = await handler.Handle(new DeletePasswordCommand(id), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.True(result.Value);
        _passwords.Verify(x => x.DeleteAsync(pwd, _action.Object), Times.Once);
    }

    [Fact]
    public async Task Handle_DeletesCatalogAndChildren_WhenCatalog()
    {
        var folderId = Guid.NewGuid();
        var folder = new PangoPassword
        {
            Id = folderId,
            Name = "F",
            IsCatalog = true,
            CatalogPath = "",
        };
        var child = new PangoPassword { Id = Guid.NewGuid(), Name = "c", CatalogPath = "F" };

        _passwords.Setup(x => x.FindAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync(folder);
        _passwords.Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync(new[] { child });

        var handler = new DeletePasswordCommandHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var result = await handler.Handle(new DeletePasswordCommand(folderId), CancellationToken.None);

        Assert.False(result.IsError);
        _passwords.Verify(x => x.DeleteAsync(child, _action.Object), Times.Once);
        _passwords.Verify(x => x.DeleteAsync(folder, _action.Object), Times.Once);
    }
}
