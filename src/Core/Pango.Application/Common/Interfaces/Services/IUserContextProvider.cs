namespace Pango.Application.Common.Interfaces.Services;

public interface IUserContextProvider
{
    string GetUserName();
    string GetSalt();
}
