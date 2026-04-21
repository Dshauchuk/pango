using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Infrastructure.Services;

/// <summary>
/// Service for generating cryptographically secure passwords based on specified options.
/// </summary>
public class PasswordGenerator(ILogger<PasswordGenerator> logger) : IPasswordGenerator
{
    private readonly ILogger<PasswordGenerator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async ValueTask<string> GeneratePasswordAsync(PasswordGenerationOptions options)
    {
        return await Task.Run(() =>
        {
            ArgumentNullException.ThrowIfNull(options);

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Length, nameof(options.Length));

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Starting password generation with options: {@Options}", options);
            }

            // Collect active sets of symbols based on user options
            var charSets = new List<string>(4);
            if (options.UseUppercase) charSets.Add(PasswordConstants.Uppercase);
            if (options.UseLowercase) charSets.Add(PasswordConstants.Lowercase);
            if (options.UseDigits) charSets.Add(PasswordConstants.Digits);
            if (options.UseSpecial) charSets.Add(PasswordConstants.Special);

            if (charSets.Count == 0)
            {
                _logger.LogWarning("Password generation failed: no character sets selected");
                throw new InvalidOperationException("At least one character type must be selected.");
            }

            // Remove ambiguous characters if requested
            if (options.ExcludeAmbiguous)
            {
                charSets = [.. charSets
                    .Select(set => new string([.. set.Except(PasswordConstants.Ambiguous)]))
                    .Where(set => set.Length > 0)];

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Ambiguous characters excluded from character sets");
                }
            }

            var allChars = string.Concat(charSets);
            var passwordChars = new List<char>(options.Length);

            using var rng = RandomNumberGenerator.Create();

            // Ensure at least one character from each selected set
            foreach (var set in charSets)
            {
                passwordChars.Add(GetRandomChar(set));
            }

            // Fill remaining length with random characters from all available sets
            for (int i = passwordChars.Count; i < options.Length; i++)
            {
                passwordChars.Add(GetRandomChar(allChars));
            }

            Shuffle(passwordChars);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Password generated successfully with length {Length}", options.Length);
            }

            return new string([.. passwordChars]);
        });
    }

    /// <summary>
    /// Gets a cryptographically secure random character from the specified character set.
    /// </summary>
    /// <param name="set">The string containing available characters to choose from.</param>
    /// <returns>A randomly selected character from the set.</returns>
    private static char GetRandomChar(string set) =>
        set[RandomNumberGenerator.GetInt32(0, set.Length)];

    /// <summary>
    /// Shuffles the list of characters using Fisher-Yates algorithm with cryptographic randomness.
    /// </summary>
    /// <param name="passwordChars">The list of characters to shuffle.</param>
    private static void Shuffle(List<char> passwordChars)
    {
        for (int i = passwordChars.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(0, i + 1);
            (passwordChars[i], passwordChars[j]) = (passwordChars[j], passwordChars[i]);
        }
    }
}
