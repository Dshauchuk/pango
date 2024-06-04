namespace Pango.Desktop.Uwp.Security;

public class AppCredentials : Persistence.ICredentials
{
    public AppCredentials(string userName, string password, string passwordSalt = null)
    {
        UserName = userName;
        PasswordHash = password;
        PasswordSalt = passwordSalt;
    }

    public string UserName { get; }

    public string PasswordHash { get; }

    public string PasswordSalt { get; }
}
