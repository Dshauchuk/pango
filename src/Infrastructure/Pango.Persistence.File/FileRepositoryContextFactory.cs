using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Persistence.File;

public class FileRepositoryContextFactory : IRepositoryContextFactory
{
    private readonly IAppDomainProvider _appDomainProvider;

    public FileRepositoryContextFactory(IAppDomainProvider appDomainProvider)
    {
        _appDomainProvider = appDomainProvider;
    }

    public IRepositoryActionContext Create(string userName, EncodingOptions encodingOptions)
    {
        string workingDirectoryPath = _appDomainProvider.GetUserFolderPath(userName);

        return new FileRepositoryActionContext(encodingOptions, userName, workingDirectoryPath);
    }
}
