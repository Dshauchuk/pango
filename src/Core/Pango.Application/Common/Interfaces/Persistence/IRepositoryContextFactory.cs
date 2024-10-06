namespace Pango.Application.Common.Interfaces.Persistence;

public interface IRepositoryContextFactory
{
    IRepositoryActionContext Create(string userName, EncodingOptions encodingOptions);
}
