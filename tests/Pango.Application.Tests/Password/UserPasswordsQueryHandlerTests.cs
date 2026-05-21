using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Password.Queries.UserPasswords;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.Password;

public class UserPasswordsQueryHandlerTests
{
    private readonly Mock<IPasswordRepository> _passwords = new();
    private readonly Mock<IUserContextProvider> _ctx = new();
    private readonly Mock<IRepositoryContextFactory> _factory = new();
    private readonly Mock<IRepositoryActionContext> _action = new();
    private readonly Mock<ILogger<UserPasswordsQueryHandler>> _logger = new();

    public UserPasswordsQueryHandlerTests()
    {
        _ctx.Setup(x => x.GetUserName()).Returns("u");
        _ctx.Setup(x => x.GetEncodingOptionsAsync()).ReturnsAsync(new EncodingOptions("k", "s"));
        _factory.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<EncodingOptions>())).Returns(_action.Object);
    }

    [Fact]
    public async Task Handle_ReturnsList_WhenSuccessful()
    {
        var items = new[] { new PangoPassword { Name = "a" }, new PangoPassword { Name = "b" } };
        _passwords.Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync(items);

        var handler = new UserPasswordsQueryHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var result = await handler.Handle(new UserPasswordsQuery(), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Count());
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenQueryThrows()
    {
        _passwords.Setup(x => x.QueryAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ThrowsAsync(new InvalidOperationException("db"));

        var handler = new UserPasswordsQueryHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var result = await handler.Handle(new UserPasswordsQuery(), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Password.QueryFailed, result.FirstError.Code);
    }
}
