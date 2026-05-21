using Pango.Application.Common.Interfaces.Services;

namespace Pango.Application.Tests.TestDoubles;

/// <summary>Deterministic fake for handlers that need hashing without Moq out-parameter gymnastics.</summary>
internal sealed class FakePasswordHashProvider : IPasswordHashProvider
{
    public string Hash(string password, byte[] salt) => "fixed-hash";

    public string Hash(string password, out byte[] salt)
    {
        salt = [1, 2, 3];
        return "hashed-master";
    }

    public bool VerifyPassword(string password, string hash, byte[] salt) =>
        password == "ok" && hash == "hash" && salt.Length > 0;
}
