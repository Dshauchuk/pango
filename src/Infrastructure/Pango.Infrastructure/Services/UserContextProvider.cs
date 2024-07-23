using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;
using Pango.Persistence;

namespace Pango.Infrastructure.Services;

public class UserContextProvider : IUserContextProvider
{
    private readonly IUserRepository _userRepository;
    private readonly IAppUserProvider _appUserProvider;

    public UserContextProvider(IUserRepository userRepository, IAppUserProvider appUserProvider)
    {
        _userRepository = userRepository;
        _appUserProvider = appUserProvider;
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
        string userId = _appUserProvider.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException();
        }

        return userId;
    }

    public async Task<string> GetKeyAsync()
    {
        PangoUser? user = await _userRepository.FindAsync(GetUserName());

        return user?.MasterPasswordHash ?? string.Empty;
    }

    public async Task<EncodingOptions> GetEncodingOptionsAsync()
    {
        return new EncodingOptions(await GetKeyAsync(), await GetSaltAsync());
    }
}
