using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;
using System.Security.Claims;
using System.Security.Principal;

namespace Pango.Infrastructure.Services;

public class UserContextProvider : IUserContextProvider
{
    private readonly IUserRepository _userRepository;

    public UserContextProvider(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Returns currently authorized user's password salt
    /// </summary>
    /// <returns></returns>
    public async Task<string> GetSaltAsync()
    {
        PangoUser? user = await _userRepository.FindAsync(GetUserName());

        return user?.PasswordSalt ?? string.Empty;
    }

    /// <summary>
    /// Returns a name of currently authorized user. Throws <see cref="UnauthorizedException"/> if there is no authorized user
    /// </summary>
    /// <returns></returns>
    /// <exception cref="UnauthorizedException">Thrown if there is no aurhorized user</exception>
    public string GetUserName()
    {
        if (Thread.CurrentPrincipal is not GenericPrincipal principal)
        {
            throw new UnauthorizedException();
        }

        return principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "Unknown";
    }
}
