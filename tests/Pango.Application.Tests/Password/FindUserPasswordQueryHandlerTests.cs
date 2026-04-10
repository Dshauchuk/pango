using Microsoft.Extensions.Logging;
using Moq;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.UseCases.Password.Queries.FindUserPassword;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.Password;

public class FindUserPasswordQueryHandlerTests
{
    private readonly Mock<IPasswordRepository> _passwords = new();
    private readonly Mock<IUserContextProvider> _ctx = new();
    private readonly Mock<IRepositoryContextFactory> _factory = new();
    private readonly Mock<IRepositoryActionContext> _action = new();
    private readonly Mock<ILogger<FindUserPasswordQueryHandler>> _logger = new();

    public FindUserPasswordQueryHandlerTests()
    {
        _ctx.Setup(x => x.GetUserName()).Returns("u");
        _ctx.Setup(x => x.GetEncodingOptionsAsync()).ReturnsAsync(new EncodingOptions("k", "s"));
        _factory.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<EncodingOptions>())).Returns(_action.Object);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenMissing()
    {
        var id = Guid.NewGuid();
        _passwords.Setup(x => x.FindAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync((PangoPassword?)null);

        var handler = new FindUserPasswordQueryHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var result = await handler.Handle(new FindUserPasswordQuery(id), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ApplicationErrors.Password.NotFound, result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenFound()
    {
        var id = Guid.NewGuid();
        var entity = new PangoPassword { Id = id, Name = "Entry", Login = "l" };
        _passwords.Setup(x => x.FindAsync(It.IsAny<Func<PangoPassword, bool>>(), _action.Object))
            .ReturnsAsync(entity);

        var handler = new FindUserPasswordQueryHandler(_passwords.Object, _ctx.Object, _factory.Object, _logger.Object);
        var result = await handler.Handle(new FindUserPasswordQuery(id), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("Entry", result.Value.Name);
    }
}
