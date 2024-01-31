using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Pango.Application.Common.Interfaces.Services;
using System.Security.Cryptography;

namespace Pango.Infrastructure.Services;

public class PasswordHashProvider : IPasswordHashProvider
{
    const int Iterations = 10000;

    public string Hash(string password, out byte[] salt)
    {
        salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        return Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: 32));
    }

    public bool VerifyPassword(string password, string hash, byte[] salt)
    {
        var passwordHash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: 32);

        var expectedHash = Convert.FromBase64String(hash);

        return expectedHash.SequenceEqual(passwordHash);
    }
}
