using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Persistence;

public class UserRepository : IUserRepository
{
    private const string CredentialsStore = "Pango.Desktop.Uwp";

    private readonly IPasswordVault _passwordVault;

    public UserRepository(IPasswordVault passwordVault)
    {
        _passwordVault = passwordVault;
    }

    public Task CreateAsync(User user)
        => _passwordVault.AddAsync(CredentialsStore, user.UserName, user.MasterPasswordHash);

    public Task DeleteAsync(User user)
        => _passwordVault.RemoveAsync(CredentialsStore, user.UserName);

    public async Task<User?> FindAsync(string userName)
    {
        ICredentials? credentials = await _passwordVault.FindAsync(CredentialsStore, userName);
        
        if(credentials is null)
        {
            return null;
        }
        return new User() { UserName = credentials.UserName, MasterPasswordHash = credentials.Password };
    }

    public async Task<IEnumerable<User>> ListAsync()
    {
        IEnumerable<string> users = await _passwordVault.ListUsersAsync(CredentialsStore);

        return users.Select(u => new User() { UserName = u });
    }
}
