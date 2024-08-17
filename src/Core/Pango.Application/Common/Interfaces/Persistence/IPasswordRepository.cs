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
    Task CreateAsync(PangoPassword password, IRepositoryActionContext context);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="passwords"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    Task CreateAsync(IEnumerable<PangoPassword> passwords, IRepositoryActionContext context);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<PangoPassword?> FindAsync(Func<PangoPassword, bool> predicate, IRepositoryActionContext context);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<IEnumerable<PangoPassword>> QueryAsync(Func<PangoPassword, bool> predicate, IRepositoryActionContext context);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    Task<PangoPassword> UpdateAsync(PangoPassword password, IRepositoryActionContext context);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    Task DeleteAsync(PangoPassword password, IRepositoryActionContext context);
}
