using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Persistence.File;

public readonly record struct FileRepositoryActionContext : IRepositoryActionContext
{
    public FileRepositoryActionContext(EncodingOptions encodingOptions, string userId, string workingDirectoryPath)
    {
        UserId = userId;
        WorkingDirectoryPath = workingDirectoryPath;
        EncodingOptions = encodingOptions;
    }

    public string UserId { get; }
    public EncodingOptions EncodingOptions { get; }
    public string WorkingDirectoryPath { get; }
}
