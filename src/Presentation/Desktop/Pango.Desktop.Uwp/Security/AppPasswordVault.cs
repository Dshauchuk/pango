using Pango.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace Pango.Desktop.Uwp.Security;

public class AppPasswordVault : IPasswordVault
{
    public async Task AddAsync(string resource, string userName, string password)
    {
        PasswordVault vault = new();
        PasswordCredential credentials = new(resource, userName, password);

        await Task.Run(() => vault.Add(credentials));
    }

    public async Task<ICredentials?> FindAsync(string resource, string userName)
    {
        PasswordVault vault = new();

        IReadOnlyList<PasswordCredential>? credentialList = null;

        try
        {
            credentialList = await Task.Run(() => vault.FindAllByResource(resource));
        }
        catch (Exception)
        {
            // TODO: add logging
            return null;
        }

        if (credentialList?.Any() is true)
        {
            foreach (var credential in credentialList)
            {
                if(credential.UserName == userName)
                {
                    credential.RetrievePassword();

                    return new AppCredentials(credential.UserName, credential.Password);
                }
            }
        }

        return null;
    }

    public async Task<IEnumerable<string>> ListUsersAsync(string resource)
    {
        PasswordVault vault = new();
        List<string> users = new()
        {
            "Alice", "Bob", "Ivan", "Dzmitry", "Alex"
        };

        IReadOnlyList<PasswordCredential>? credentialList = null;

        try
        {
            credentialList = await Task.Run(() => vault.FindAllByResource(resource));
        }
        catch (Exception)
        {
            // TODO: add logging
            return users;
        }

        if (credentialList?.Any() is true)
        {
            foreach (var credential in credentialList)
            {
                //credential.RetrievePassword();
                users.Add(credential.UserName);
            }
        }

        return users;
    }

    public async Task RemoveAsync(string resource, string userName)
    {
        PasswordVault vault = new();

        PasswordCredential existentCredentials =
            vault
            .FindAllByResource(resource)
            .FirstOrDefault(c => c.UserName == userName);

        if (existentCredentials != null)
        {
            await Task.Run(() => vault.Remove(existentCredentials));
        }
    }
}
