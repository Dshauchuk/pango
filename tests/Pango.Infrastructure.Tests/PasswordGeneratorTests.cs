using Microsoft.Extensions.Logging.Abstractions;
using Pango.Application.Common;
using Pango.Infrastructure.Services;

namespace Pango.Infrastructure.Tests
{
    public class PasswordGeneratorTests
    {
        private readonly PasswordGenerator _passwordGenerator;

        public PasswordGeneratorTests()
        {
            _passwordGenerator = new PasswordGenerator(NullLogger<PasswordGenerator>.Instance);
        }

        private static PasswordGenerationOptions CreateOptions(
            int length = 16,
            bool useUpper = true,
            bool useLower = true,
            bool useDigits = true,
            bool useSpecial = false,
            bool excludeAmbiguous = false)
        {
            return new PasswordGenerationOptions(
                length,
                useUpper,
                useLower,
                useDigits,
                useSpecial,
                excludeAmbiguous);
        }

        [Theory]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(64)]
        public async Task GeneratePasswordAsync_LengthIsRespected(int length)
        {
            // Arrange
            var options = CreateOptions(length: length);

            // Act
            var password = await _passwordGenerator.GeneratePasswordAsync(options);

            // Assert
            Assert.Equal(length, password.Length);
        }

        [Fact]
        public async Task GeneratePasswordAsync_UppercaseOnly_ContainsOnlyUppercase()
        {
            // Arrange
            var options = CreateOptions(
                length: 16,
                useUpper: true,
                useLower: false,
                useDigits: false,
                useSpecial: false);

            // Act
            var password = await _passwordGenerator.GeneratePasswordAsync(options);

            // Assert
            Assert.NotEmpty(password);
            Assert.All(password, c => Assert.InRange(c, 'A', 'Z'));
        }

        [Fact]
        public async Task GeneratePasswordAsync_LowercaseOnly_ContainsOnlyLowercase()
        {
            var options = CreateOptions(
                length: 16,
                useUpper: false,
                useLower: true,
                useDigits: false,
                useSpecial: false);

            var password = await _passwordGenerator.GeneratePasswordAsync(options);

            Assert.NotEmpty(password);
            Assert.All(password, c => Assert.InRange(c, 'a', 'z'));
        }
        [Fact]
        public async Task GeneratePasswordAsync_DigitsOnly_ContainsOnlyDigits()
        {
            var options = CreateOptions(
                length: 16,
                useUpper: false,
                useLower: false,
                useDigits: true,
                useSpecial: false);

            var password = await _passwordGenerator.GeneratePasswordAsync(options);

            Assert.NotEmpty(password);
            Assert.All(password, c => Assert.InRange(c, '0', '9'));
        }
        [Fact]
        public async Task GeneratePasswordAsync_UpperLowerDigits_ContainsAllSelectedTypes()
        {
            var options = CreateOptions(
                length: 16,
                useUpper: true,
                useLower: true,
                useDigits: true,
                useSpecial: false);

            var password = await _passwordGenerator.GeneratePasswordAsync(options);

            Assert.NotEmpty(password);
            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, char.IsDigit);
        }
        [Fact]
        public async Task GeneratePasswordAsync_SpecialIncluded_ContainsAtLeastOneSpecial()
        {
            var options = CreateOptions(
                length: 16,
                useUpper: false,
                useLower: false,
                useDigits: false,
                useSpecial: true);

            var password = await _passwordGenerator.GeneratePasswordAsync(options);

            Assert.NotEmpty(password);

            const string special = "!@#$%^&*()-_=+[]{};:,.<>/?"; 
            Assert.Contains(password, c => special.Contains(c));
        }
        [Fact]
        public async Task GeneratePasswordAsync_ExcludeAmbiguous_RemovesAmbiguousCharacters()
        {
            var options = CreateOptions(
                length: 32,
                useUpper: true,
                useLower: true,
                useDigits: true,
                useSpecial: false,
                excludeAmbiguous: true);

            var password = await _passwordGenerator.GeneratePasswordAsync(options);

            const string ambiguous = "0O1Il|"; 

            Assert.DoesNotContain(password, c => ambiguous.Contains(c));
        }
        [Fact]
        public async Task GeneratePasswordAsync_MultipleCalls_ProduceDifferentPasswords()
        {
            var options = CreateOptions(length: 16);

            var p1 = await _passwordGenerator.GeneratePasswordAsync(options);
            var p2 = await _passwordGenerator.GeneratePasswordAsync(options);
            var p3 = await _passwordGenerator.GeneratePasswordAsync(options);

            Assert.False(p1 == p2 && p2 == p3);
        }

        [Fact]
        public async Task GeneratePasswordAsync_NullOptions_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _passwordGenerator.GeneratePasswordAsync(null!).AsTask());
        }

        [Fact]
        public async Task GeneratePasswordAsync_NonPositiveLength_Throws()
        {
            var options = new PasswordGenerationOptions(0, true, false, false, false, false);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await _passwordGenerator.GeneratePasswordAsync(options).AsTask());
        }

        [Fact]
        public async Task GeneratePasswordAsync_NoCharsAfterFiltering_Throws()
        {
            var options = new PasswordGenerationOptions(10, false, false, false, false, true);

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _passwordGenerator.GeneratePasswordAsync(options).AsTask());
        }
    }
}
