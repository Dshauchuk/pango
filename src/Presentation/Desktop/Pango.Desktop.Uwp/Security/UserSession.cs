using System.Linq;
using Windows.Security.Credentials;

namespace Pango.Desktop.Uwp.Security
{
    public class SecureUserSession
    {
        private const string ResourceName = "Pango.Desktop.Uwp.Session";

        public static void SaveUser(string username)
        {
            var vault = new PasswordVault();
            vault.Add(new PasswordCredential(ResourceName, username, username));
        }

        public static PasswordCredential GetUser()
        {
            var vault = new PasswordVault();
            try
            {
                var credential = vault.FindAllByResource(ResourceName).FirstOrDefault();
                if (credential != null)
                {
                    credential.RetrievePassword();
                    return credential;
                }
            }
            catch
            {
                // Handle errors (e.g., no credentials found)
            }
            return null;
        }

        public static void ClearUser()
        {
            try
            {
                var vault = new PasswordVault();
                var credentials = vault.FindAllByResource(ResourceName);
                foreach (var credential in credentials)
                {
                    vault.Remove(credential);
                }
            }
            catch
            {
                // Handle errors (e.g., no credentials found)
            }
        }
    }
}
