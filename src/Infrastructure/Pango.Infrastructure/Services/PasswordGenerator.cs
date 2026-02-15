using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using System.Security.Cryptography;

namespace Pango.Infrastructure.Services;

public class PasswordGenerator : IPasswordGenerator
{
    public ValueTask<string> GeneratePasswordAsync(PasswordGenerationOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        if (options.Length <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.Length));

        // collect active sets of symbols
        var charSets = new List<string>();
        if (options.UseUppercase) charSets.Add(PasswordConstants.Uppercase);
        if (options.UseLowercase) charSets.Add(PasswordConstants.Lowercase);
        if (options.UseDigits) charSets.Add(PasswordConstants.Digits);
        if (options.UseSpecial) charSets.Add(PasswordConstants.Special);

        if (charSets.Count == 0)
            throw new InvalidOperationException("No characters available for password generation.");

        if (options.ExcludeAmbiguous) 
        {
            charSets = charSets
                .Select(set => new string(set.Except(PasswordConstants.Ambiguous).ToArray()))
                .Where(set => set.Length > 0)
                .ToList();
        }

        var allChars = string.Concat(charSets);
        var passwordChars = new List<char>(options.Length);

        // ensure existence char from each set
        using var rng = RandomNumberGenerator.Create();
        foreach(var set in charSets)
        {
            passwordChars.Add(GetRandomChar(set, rng));
        }

        // get other symbols from general pull
        for(int i = passwordChars.Count; i < options.Length; i++)
        {
            passwordChars.Add(GetRandomChar(allChars, rng));
        }

        // shuffle
        Shuffle(passwordChars, rng);
        var password = new string(passwordChars.ToArray());

        return ValueTask.FromResult(password);
    }
    private char GetRandomChar(string set, RandomNumberGenerator rng)
    {
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var value = BitConverter.ToUInt32(bytes, 0);
        var index = (int)(value % (uint)set.Length);
        return set[index];
    }
    private void Shuffle(List<char> passwordChars, RandomNumberGenerator rng)
    {
        for (int i = passwordChars.Count - 1; i > 0; i--)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var value = BitConverter.ToUInt32(bytes, 0);
            var j = (int)(value % (uint)(i + 1));

            (passwordChars[i], passwordChars[j]) = (passwordChars[j], passwordChars[i]);
        }
    }
}
