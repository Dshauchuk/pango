using Pango.Domain.Entities;

namespace Pango.Application.Common.Interfaces.Persistence;

/// <summary>
/// 
/// </summary>
public interface IUserRepository : IRepository
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task CreateAsync(User user);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<User?> FindAsync(string userName);
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<User>> ListAsync();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task DeleteAsync(User user);
}
