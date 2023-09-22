using Pango.Application.Models;

namespace Pango.Application.Common.Interfaces.Persistence;

public interface IUserRepository : IRepository
{
    Task CreateAsync(UserDto userDto);
    Task<UserDto> FindAsync(Func<UserDto, bool> predicate);
    Task<IEnumerable<UserDto>> ListAsync();
    Task DeleteAsync(UserDto userDto);
}
