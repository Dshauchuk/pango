using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;

namespace Pango.Infrastructure.Data;

public class UserRepository : IUserRepository
{
    private readonly IEnumerable<UserDto> _users =
        new List<UserDto>()
        {
            new UserDto { Id = Guid.NewGuid(), Username = "John Doe" },
            new UserDto { Id = Guid.NewGuid(), Username = "Jane Doe" }
        };

    public Task CreateAsync(UserDto userDto)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(UserDto userDto)
    {
        throw new NotImplementedException();
    }

    public Task<UserDto> FindAsync(Func<UserDto, bool> predicate)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<UserDto>> ListAsync()
    {
        return Task.FromResult(_users);
    }
}
