using Pango.Application.Common.Interfaces.Services;
using System.Security.Cryptography;

namespace Pango.Infrastructure.Services;

// TODO: complete the implementation
public class PasswordHashProvider : IPasswordHashProvider
{
    const int KeySize = 64;
    const int Iterations = 350000;
    HashAlgorithmName _hashAlgorithm = HashAlgorithmName.SHA512;

    // TODO: complete the implementation
    public string Hash(string password, out byte[] salt)
    {
        salt = new byte[KeySize];

        return password;
    }

    // TODO: complete the implementation
    public bool VerifyPassword(string password, string hash, byte[] salt)
    {
        return hash == password;
    }
}
