using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Persistence;

/// <summary>
/// <see cref="UserRepository"/> is a password vault repository and doesn't interact with the file system directly.
/// All user data are stored in the file system, so need a separate file system based repository to interact with user data (remove user data)
/// </summary>
public class UserDataRepository : FileRepositoryBase<PangoUser>, IUserDataRepository
{
    // user data doesn't have a separate file
    protected override string FileName => string.Empty;

    public UserDataRepository(IContentEncoder contentEncoder, IAppDomainProvider appDomainProvider)
        : base(contentEncoder, appDomainProvider)
    {
    }

    /// <inheritdoc/>
    public Task DeleteAllUserDataAsync(string userId)
        => DeleteUserDataAsync(userId);
}
