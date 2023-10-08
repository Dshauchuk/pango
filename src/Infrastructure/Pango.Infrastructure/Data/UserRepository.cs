using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;
using Windows.Security.Credentials;

namespace Pango.Infrastructure.Data;

public class UserRepository : IUserRepository
{
    private const string CredentialsStore = "Pango.Desktop.Uwp";

    public async Task CreateAsync(User user)
    {
        PasswordVault vault = new();
        PasswordCredential credentials = new(CredentialsStore, user.UserName, user.MasterPasswordHash);

        await Task.Run(() => vault.Add(credentials));
    }

    public async Task DeleteAsync(User user)
    {
        PasswordVault vault = new();

        PasswordCredential existentCredentials = 
            vault
            .FindAllByResource(CredentialsStore)
            .FirstOrDefault(c => c.UserName == user.UserName);

        if (existentCredentials != null)
        {
            await Task.Run(() => vault.Remove(existentCredentials));
        }
    }

    public async Task<User> FindAsync(Func<User, bool> predicate)
        => (await ListAsync()).FirstOrDefault(predicate);

    public async Task<IEnumerable<User>> ListAsync()
    {
        List<User> users = new();
        PasswordVault vault = new ();

        IReadOnlyList<PasswordCredential>? credentialList = null;

        try
        {
            credentialList = await Task.Run(() => vault.FindAllByResource(CredentialsStore));
        }
        catch (Exception)
        {
            // TODO: add logging
            return users;
        }

        if (credentialList.Any() is true)
        {
            foreach(var credential in credentialList)
            {
                credential.RetrievePassword();
                users.Add(new User() { UserName = credential.UserName, MasterPasswordHash = credential.Password });
            }
        }

        return users;
    }
}
