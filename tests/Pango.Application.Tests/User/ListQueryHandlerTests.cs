using Moq;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.UseCases.User.Queries.List;
using Pango.Domain.Entities;

namespace Pango.Application.Tests.User;

public class ListQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsUsers_NaturalSortedByName()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.ListAsync()).ReturnsAsync(new[]
        {
            new PangoUser { UserName = "b" },
            new PangoUser { UserName = "a10" },
            new PangoUser { UserName = "a2" },
            new PangoUser { UserName = "a" },
        });

        var handler = new ListQueryHandler(users.Object);
        var result = await handler.Handle(new ListQuery(), CancellationToken.None);

        Assert.False(result.IsError);
        var names = result.Value.Select(u => u.UserName).ToList();
        Assert.Equal(new[] { "a", "a2", "a10", "b" }, names);
    }
}
