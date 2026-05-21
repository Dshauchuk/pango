using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Password.Commands.ToggleStar;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.Password;

public class TogglePasswordStarCommandHandlerTests
{
    private readonly Mock<IPasswordRepository> _passwords = new();
    private readonly Mock<IUserContextProvider> _ctx = new();
    private readonly Mock<IRepositoryContextFactory> _factory = new();
    private readonly Mock<IRepositoryActionContext> _action = new();
    private readonly Mock<ILogger<TogglePasswordStarCommandHandler>> _logger = new();

    public TogglePasswordStarCommandHandlerTests()
    {
        _ctx.Setup(x => x.GetUserName()).Returns("u");
        _ctx.Setup(x => x.GetEncodingOptionsAsync()).ReturnsAsync(new EncodingOptions("k", "s"));
        _factory.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<EncodingOptions>())).Returns(_action.Object);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenPasswordMissing()
    {
        _passwords.Setup(x => x.FindAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync((PangoPassword?)null);

        var handler = new TogglePasswordStarCommandHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var result = await handler.Handle(new TogglePasswordStarCommand(Guid.NewGuid(), true), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Password.NotFound, result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_UpdatesStar_WhenFound()
    {
        var id = Guid.NewGuid();
        var pwd = new PangoPassword { Id = id, Star = false };
        _passwords.Setup(x => x.FindAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync(pwd);

        var handler = new TogglePasswordStarCommandHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var result = await handler.Handle(new TogglePasswordStarCommand(id, true), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.True(result.Value);
        Assert.True(pwd.Star);
        _passwords.Verify(x => x.UpdateAsync(pwd, _action.Object), Times.Once);
    }
}
