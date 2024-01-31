namespace Pango.Persistence;

public interface ICredentials
{
    string UserName { get; }    
    string PasswordHash { get; }
    string PasswordSalt { get; }
}
