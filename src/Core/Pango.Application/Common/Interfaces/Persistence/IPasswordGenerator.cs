namespace Pango.Application.Common.Interfaces.Persistence;

public interface IPasswordGenerator
{
    ValueTask<string> GeneratePasswordAsync(PasswordGenerationOptions options);
}
