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
    Task CreateAsync(PangoPassword password);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<PangoPassword?> FindAsync(string userName, Func<PangoPassword, bool> predicate);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<IEnumerable<PangoPassword>> QueryAsync(string userName, Func<PangoPassword, bool> predicate);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    Task<PangoPassword> UpdateAsync(PangoPassword password);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    Task DeleteAsync(PangoPassword password);
}
