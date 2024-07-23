using Newtonsoft.Json;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
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

                    Dictionary<string, object> securedContent = 
                        JsonConvert.DeserializeObject<Dictionary<string, object>>(credential.Password) ?? throw new PangoException(ApplicationErrors.User.UnkownError, $"Cannot retrieve password for user \"{userName}\"");

                    return new AppCredentials(
                        credential.UserName, 
                        securedContent[UserProperties.Password].ToString() ?? throw new PangoException(ApplicationErrors.User.UnkownError, $"Cannot retrieve password for user \"{userName}\""), 
                        securedContent[UserProperties.PasswordSalt].ToString() ?? throw new PangoException(ApplicationErrors.User.UnkownError, $"Cannot retrieve password salt for user \"{userName}\""));
                }
            }
        }

        return null;
    }

    public async Task<IEnumerable<string>> ListUsersAsync(string resource)
    {
        PasswordVault vault = new();
        List<string> users = [];

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
                users.Add(credential.UserName);
            }
        }

        return users;
    }

    public async Task RemoveAsync(string resource, string userName)
    {
        PasswordVault vault = new();

        PasswordCredential? existentCredentials =
            vault
            .FindAllByResource(resource)
            .FirstOrDefault(c => c.UserName == userName);

        if (existentCredentials != null)
        {
            await Task.Run(() => vault.Remove(existentCredentials));
        }
    }
}
