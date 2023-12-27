namespace Pango.Desktop.Uwp.Security;

public class AppCredentials : Persistence.ICredentials
{
    public AppCredentials(string userName, string password)
    {
        UserName = userName;
        Password = password;
    }

    public string UserName { get; }

    public string Password { get; }
}
