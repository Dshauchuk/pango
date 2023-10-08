using Pango.Domain.Entities;

namespace Pango.Application.Common.Interfaces.Persistence;

public interface IUserRepository : IRepository
{
    Task CreateAsync(User user);
    Task<User> FindAsync(Func<User, bool> predicate);
    Task<IEnumerable<User>> ListAsync();
    Task DeleteAsync(User user);
}
