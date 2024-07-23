using Microsoft.Extensions.Logging;
using Pango.Application.Common.Interfaces;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Persistence.File;

/// <summary>
/// <see cref="UserRepository"/> is a password vault repository and doesn't interact with the file system directly.
/// All user data are stored in the file system, so need a separate file system based repository to interact with user data (remove user data)
/// </summary>
public class UserDataRepository : FileRepositoryBase<PangoUser>, IUserDataRepository
{
    // user data doesn't have a separate file
    protected override string DirectoryName => "users";

    public UserDataRepository(IContentEncoder contentEncoder, 
        IAppDomainProvider appDomainProvider,
        IAppOptions appOptions,
        ILogger<UserDataRepository> logger)
        : base(contentEncoder, appOptions, logger)
    {
    }

    /// <inheritdoc/>
    public Task DeleteAllUserDataAsync(string userDirectoryPath)
        => DeleteDataAsync(userDirectoryPath);
}
