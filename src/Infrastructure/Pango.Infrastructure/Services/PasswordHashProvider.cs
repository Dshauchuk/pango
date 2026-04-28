using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Services;
using System.Security.Cryptography;

namespace Pango.Infrastructure.Services;

public class PasswordHashProvider : IPasswordHashProvider
{
    const int Iterations = 10000;

    public string Hash(string password, byte[] salt)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentNullException(nameof(password), "Password must be specified");
        }

        return Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: AppConstants.Security.Pbkdf2HashIterations,
            numBytesRequested: AppConstants.Security.AesKeySize));
    }

    public string Hash(string password, out byte[] salt)
    {
        salt = new byte[AppConstants.Security.AesIvSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        return Hash(password, salt);
    }

    public bool VerifyPassword(string password, string hash, byte[] salt)
    {
        if(string.IsNullOrEmpty(hash)) 
        {
            return false;
        }

        if(salt is null || salt.Length == 0)
        {
            return false;
        }

        var passwordHash = KeyDerivation.Pbkdf2( 
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: AppConstants.Security.AesKeySize);

        var expectedHash = Convert.FromBase64String(hash);

        return expectedHash.SequenceEqual(passwordHash);
    }
}
