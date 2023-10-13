using Pango.Persistence;

namespace Pango.Infrastructure;

public class RepositoryContext : IRepositoryContext
{
    public RepositoryContext(string userId)
    {
        UserId = userId;
    }

    public string UserId { get; }
}
