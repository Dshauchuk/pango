namespace Pango.Application.Common.Interfaces.Services;

public interface IPasswordHashProvider
{
    string Hash(string password, byte[] salt);

    string Hash(string password, out byte[] salt);

    bool VerifyPassword(string password, string hash, byte[] salt);
}
