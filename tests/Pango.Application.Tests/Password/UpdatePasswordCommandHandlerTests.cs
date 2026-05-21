using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Password.Commands.UpdatePassword;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.Password;

public class UpdatePasswordCommandHandlerTests
{
    private readonly Mock<IPasswordRepository> _passwords = new();
    private readonly Mock<IUserContextProvider> _ctx = new();
    private readonly Mock<IRepositoryContextFactory> _factory = new();
    private readonly Mock<IRepositoryActionContext> _action = new();
    private readonly Mock<ILogger<UpdatePasswordCommandHandler>> _logger = new();

    public UpdatePasswordCommandHandlerTests()
    {
        _ctx.Setup(x => x.GetUserName()).Returns("u");
        _ctx.Setup(x => x.GetEncodingOptionsAsync()).ReturnsAsync(new EncodingOptions("k", "s"));
        _factory.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<EncodingOptions>())).Returns(_action.Object);
    }

    [Fact]
    public async Task Handle_UpdatesRegularEntry_WhenNotCatalog()
    {
        var id = Guid.NewGuid();
        var existing = new PangoPassword { Id = id, Name = "Old", IsCatalog = false, CatalogPath = "Root" };
        _passwords.Setup(x => x.FindAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync(existing);
        _passwords.Setup(x => x.UpdateAsync(It.IsAny<PangoPassword>(), _action.Object))
            .ReturnsAsync((PangoPassword p, IRepositoryActionContext _) => p);

        var cmd = new UpdatePasswordCommand(id, "NewName", "log", "val", true);
        cmd.CatalogPath = "Other";

        var handler = new UpdatePasswordCommandHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("NewName", result.Value.Name);
        Assert.Equal("log", result.Value.Login);
        Assert.Equal("Other", existing.CatalogPath);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenMissing()
    {
        _passwords.Setup(x => x.FindAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync((PangoPassword?)null);

        var handler = new UpdatePasswordCommandHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var result = await handler.Handle(new UpdatePasswordCommand(Guid.NewGuid(), "n", "l", "v"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Password.NotFound, result.FirstError.Code);
    }
}
