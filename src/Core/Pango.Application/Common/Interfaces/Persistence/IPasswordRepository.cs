using Pango.Domain.Entities;

namespace Pango.Application.Common.Interfaces.Persistence;

/// <summary>
/// 
/// </summary>
public interface IPasswordRepository
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    Task CreateAsync(Password password);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<Password> FindAsync(Func<Password, bool> predicate);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<IEnumerable<Password>> QueryAsync(Func<Password, bool> predicate);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    Task DeleteAsync(Password password);
}
