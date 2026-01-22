namespace Pango.Application.Common.Interfaces.Persistence
{
    public interface IPasswordGenerator
    {
        Task<string> GeneratePassword(PasswordGenerationOptions options);
    }
}
