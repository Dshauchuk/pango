using Moq;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.UseCases.User.Queries.FindUser;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.User;

public class FindUserQueryHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Handle_ReturnsValidation_WhenNameMissing(string? name)
    {
        var handler = new FindUserQueryHandler(_users.Object);
        var result = await handler.Handle(new FindUserQuery { Name = name }, CancellationToken.None);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _users.Setup(x => x.FindAsync("nobody")).ReturnsAsync((PangoUser?)null);
        var handler = new FindUserQueryHandler(_users.Object);

        var result = await handler.Handle(new FindUserQuery("nobody"), CancellationToken.None);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Handle_ReturnsDto_WhenUserExists()
    {
        _users.Setup(x => x.FindAsync("alice")).ReturnsAsync(new PangoUser { UserName = "alice" });
        var handler = new FindUserQueryHandler(_users.Object);

        var result = await handler.Handle(new FindUserQuery("alice"), CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("alice", result.Value.UserName);
    }
}
