using Newtonsoft.Json;
using Pango.Application.Common;
using Pango.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials;

namespace Pango.Desktop.Uwp.Security;

public class AppPasswordVault : IPasswordVault
{
    public async Task AddAsync(string resource, string userName, string password, IDictionary<string, object>? properties = null)
    {
        PasswordVault vault = new();

        properties ??= new Dictionary<string, object>();    
        Dictionary<string, object> securedContent = new(properties)
        {
            { UserProperties.Password, password }
        };

        PasswordCredential credentials = new(resource, userName, JsonConvert.SerializeObject(securedContent));

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

                    Dictionary<string, object> securedContent = JsonConvert.DeserializeObject<Dictionary<string, object>>(credential.Password);

                    return new AppCredentials(credential.UserName, securedContent[UserProperties.Password].ToString(), securedContent[UserProperties.PasswordSalt].ToString());
                }
            }
        }

        return null;
    }

    public async Task<IEnumerable<string>> ListUsersAsync(string resource)
    {
        PasswordVault vault = new();
        List<string> users = new();

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
