namespace Pango.Persistence;

public interface IPasswordVault
{
    Task AddAsync(string resource, string userName, string password, IDictionary<string, object>? properties = null);

    Task<ICredentials?> FindAsync(string resource, string userName);
    
    Task RemoveAsync(string resource, string userName);

    Task<IEnumerable<string>> ListUsersAsync(string resource);
}
