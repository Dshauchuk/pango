using Pango.Application.Common;
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

    public Task CreateAsync(PangoUser user)
        => _passwordVault.AddAsync(CredentialsStore, user.UserName, user.MasterPasswordHash, new Dictionary<string, object>() { { UserProperties.PasswordSalt, user.PasswordSalt ?? string.Empty } });

    public Task DeleteAsync(PangoUser user)
        => _passwordVault.RemoveAsync(CredentialsStore, user.UserName);

    public async Task<PangoUser?> FindAsync(string userName)
    {
        ICredentials? credentials = await _passwordVault.FindAsync(CredentialsStore, userName);
        
        if(credentials is null)
        {
            return null;
        }
        return new PangoUser() { UserName = credentials.UserName, MasterPasswordHash = credentials.PasswordHash, PasswordSalt = credentials.PasswordSalt };
    }

    public async Task<IEnumerable<PangoUser>> ListAsync()
    {
        IEnumerable<string> users = await _passwordVault.ListUsersAsync(CredentialsStore);

        return users.Select(u => new PangoUser() { UserName = u });
    }
}
