using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Password.Commands.NewPassword;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.Password;

public class NewPasswordCommandHandlerTests
{
    private readonly Mock<IPasswordRepository> _passwords = new();
    private readonly Mock<IUserContextProvider> _ctx = new();
    private readonly Mock<IRepositoryContextFactory> _factory = new();
    private readonly Mock<IRepositoryActionContext> _action = new();
    private readonly Mock<ILogger<NewPasswordCommandHandler>> _logger = new();

    public NewPasswordCommandHandlerTests()
    {
        _ctx.Setup(x => x.GetUserName()).Returns("alice");
        _ctx.Setup(x => x.GetEncodingOptionsAsync()).ReturnsAsync(new EncodingOptions("k", "s"));
        _factory.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<EncodingOptions>())).Returns(_action.Object);
    }

    [Fact]
    public async Task Handle_CreatesPassword_AndReturnsDto()
    {
        PangoPassword? captured = null;
        _passwords.Setup(x => x.CreateAsync(It.IsAny<PangoPassword>(), _action.Object))
            .Callback<PangoPassword, IRepositoryActionContext>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        var handler = new NewPasswordCommandHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var cmd = new NewPasswordCommand("MyPass", "login1", "secret")
        {
            CatalogPath = "Work",
            IsCatalogHolder = false,
            Star = true,
        };

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("MyPass", result.Value.Name);
        Assert.Equal("login1", result.Value.Login);
        Assert.NotNull(captured);
        Assert.Equal("alice", captured!.UserName);
        Assert.Equal("Work", captured.CatalogPath);
        Assert.True(captured.Star);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenRepositoryThrows()
    {
        _passwords.Setup(x => x.CreateAsync(It.IsAny<PangoPassword>(), _action.Object))
            .ThrowsAsync(new InvalidOperationException("io"));

        var handler = new NewPasswordCommandHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var result = await handler.Handle(new NewPasswordCommand("n", "l", "v"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Password.CreationFailed, result.FirstError.Code);
    }
}
