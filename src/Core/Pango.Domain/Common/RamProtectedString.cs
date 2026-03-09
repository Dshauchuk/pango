using System.Security.Cryptography;
using System.Text;

namespace Pango.Domain.Common;

/// <summary>
/// Represents a string that is encrypted while residing in RAM.
/// </summary>
public class RamProtectedString : IDisposable
{
    private static readonly byte[] SessionKey = new byte[32];
    private byte[]? _encryptedData;
    private byte[]? _iv;

    /// <summary>
    /// Initializes the static session key securely once per application run.
    /// </summary>
    static RamProtectedString()
    {
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(SessionKey);
    }

    public RamProtectedString() { }

    public RamProtectedString(string? plainText = null)
    {
        SetPlaintextValue(plainText);
    }

    ~RamProtectedString()
    {
        Dispose();
    }

    /// <summary>
    /// Encrypts and stores the provided plaintext string in memory.
    /// </summary>
    public void SetPlaintextValue(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            ClearMemory();
            return;
        }

        _iv = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(_iv);

        using var aes = Aes.Create();
        aes.Key = SessionKey;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        try
        {
            _encryptedData = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainBytes);
        }
    }

    /// <summary>
    /// Decrypts the stored data and returns the plaintext string.
    /// </summary>
    public string GetDecryptedValue()
    {
        if (_encryptedData == null || _iv == null) return string.Empty;

        using var aes = Aes.Create();
        aes.Key = SessionKey;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor();
        byte[]? plainBytes = null;
        try
        {
            plainBytes = decryptor.TransformFinalBlock(_encryptedData, 0, _encryptedData.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
        finally
        {
            if (plainBytes != null)
            {
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        }
    }

    private void ClearMemory()
    {
        if (_encryptedData != null)
        {
            CryptographicOperations.ZeroMemory(_encryptedData);
            _encryptedData = null;
        }
        if (_iv != null)
        {
            CryptographicOperations.ZeroMemory(_iv);
            _iv = null;
        }
    }

    public void Dispose()
    {
        ClearMemory();
        GC.SuppressFinalize(this);
    }
}