namespace Pango.Application.Common.Interfaces.Services;

public interface IUserContextProvider
{
    string GetUserName();
    Task<string> GetSaltAsync();
    Task<string> GetKeyAsync();
}
