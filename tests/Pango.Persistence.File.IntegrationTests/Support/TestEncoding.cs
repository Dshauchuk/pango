using System.Security.Cryptography;
using Pango.Application.Common;

namespace Pango.Persistence.File.IntegrationTests.Support;

internal static class TestEncoding
{
    /// <summary>Produces key/IV pairs compatible with <see cref="Pango.Infrastructure.Services.ContentEncoder"/>.</summary>
    public static EncodingOptions CreateRandomAes256()
    {
        var key = new byte[32];
        var iv = new byte[16];
        RandomNumberGenerator.Fill(key);
        RandomNumberGenerator.Fill(iv);
        return new EncodingOptions(Convert.ToBase64String(key), Convert.ToBase64String(iv));
    }
}
