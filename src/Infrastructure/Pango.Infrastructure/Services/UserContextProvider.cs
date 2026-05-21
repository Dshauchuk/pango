using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;
using Pango.Persistence;

namespace Pango.Infrastructure.Services;

public class UserContextProvider(IUserRepository userRepository, IAppUserProvider appUserProvider) : IUserContextProvider
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IAppUserProvider _appUserProvider = appUserProvider;

    // Cache for current session data
    private EncodingOptions? _cachedEncodingOptions;
    private string? _cachedUserName;

    /// <summary>
    /// Returns the name of the authorized user. 
    /// Automatically resets cache if the user ID changes or is cleared.
    /// </summary>
    public string GetUserName()
    {
        // Get the current ID from the provider (e.g. from SecureUserSession)
        string currentId = _appUserProvider.GetUserId();

        // If the ID changed (user logged out or switched), reset the local cache
        if (currentId != _cachedUserName)
        {
            _cachedUserName = currentId;
            _cachedEncodingOptions = null;
        }

        if (string.IsNullOrEmpty(_cachedUserName))
            throw new UnauthorizedException();

        return _cachedUserName;
    }

    /// <summary>
    /// Retrieves encoding options (key/salt). Uses cache if available.
    /// </summary>
    public async Task<EncodingOptions> GetEncodingOptionsAsync()
    {
        // Return cached options if they exist and user hasn't changed
        string currentId = GetUserName();

        if (_cachedEncodingOptions.HasValue)
            return _cachedEncodingOptions.Value;

        // Fetch user from repository to get master hash and salt
        PangoUser? user = await _userRepository.FindAsync(currentId) ?? throw new UnauthorizedException();

        // Store result in cache
        _cachedEncodingOptions = new EncodingOptions(user.MasterPasswordHash, user.PasswordSalt);
        return _cachedEncodingOptions.Value;
    }

    // Helper methods to access specific encoding parts
    public async Task<string> GetSaltAsync() => (await GetEncodingOptionsAsync()).Salt;
    public async Task<string> GetKeyAsync() => (await GetEncodingOptionsAsync()).Key;
}
