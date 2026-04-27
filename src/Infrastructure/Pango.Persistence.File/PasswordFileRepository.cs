using Microsoft.Extensions.Logging;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

#pragma warning disable CA1873

namespace Pango.Persistence.File;

/// <summary>
/// File-based implementation of the password repository.
/// Provides CRUD operations with in-memory caching per user session.
/// </summary>
public class PasswordFileRepository(
    IContentEncoder contentEncoder,
    IAppDomainProvider appDomainProvider,
    ILogger<PasswordFileRepository> logger,
    IAppOptions appOptions)
    : FileRepositoryBase<PangoPassword>(contentEncoder, appDomainProvider, appOptions, logger), IPasswordRepository
{
    protected override string DirectoryName => "passwords";

    private readonly Lock _cacheLock = new();
    private List<PangoPassword>? _sessionCache;
    private string? _cachedUserId;

    /// <summary>
    /// Retrieves the cached list of passwords for the current user, or loads it from disk if missing.
    /// </summary>
    private async Task<List<PangoPassword>> GetOrLoadCacheAsync(FileRepositoryActionContext ctx)
    {
        lock (_cacheLock)
        {
            if (_sessionCache != null && _cachedUserId == ctx.UserId)
                return _sessionCache;
        }

        logger.LogDebug("Cache miss for user {UserId}. Loading passwords from disk.", ctx.UserId);
        var items = await ExtractAllItemsForUserAsync(Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions);

        lock (_cacheLock)
        {
            _sessionCache = [.. items];
            _cachedUserId = ctx.UserId;
        }

        return _sessionCache;
    }

    /// <summary>
    /// Persists the current in-memory cache to disk if it contains data.
    /// </summary>
    private async Task FlushCacheToDiskAsync(FileRepositoryActionContext ctx)
    {
        List<PangoPassword> itemsToSave;
        lock (_cacheLock)
        {
            if (_sessionCache == null)
                return;

            itemsToSave = [.. _sessionCache];
        }

        logger.LogDebug("Saving {Count} password(s) to disk for user {UserId}.", itemsToSave.Count, ctx.UserId);
        await SaveItemsForUserAsync(itemsToSave, ctx.UserId, Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions);
    }

    /// <summary>
    /// Creates a new password record and persists it to storage.
    /// </summary>
    public async Task CreateAsync(PangoPassword password, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
            throw new ArgumentException($"Invalid context type. Expected {typeof(FileRepositoryActionContext).FullName}.", nameof(context));

        logger.LogDebug("Creating new password with ID {PasswordId} for user {UserId}.", password.Id, ctx.UserId);

        var passwordList = await GetOrLoadCacheAsync(ctx);
        password.UserName = ctx.UserId;
        passwordList.Add(password);

        await FlushCacheToDiskAsync(ctx);
        logger.LogInformation("Password {PasswordId} created successfully.", password.Id);
    }

    /// <summary>
    /// Creates multiple password records and persists them to storage.
    /// </summary>
    public async Task CreateAsync(IEnumerable<PangoPassword> passwords, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
            throw new ArgumentException($"Invalid context type. Expected {typeof(FileRepositoryActionContext).FullName}.", nameof(context));

        var passwordArray = passwords.ToArray();
        if (passwordArray.Length == 0)
            return;

        logger.LogDebug("Creating {Count} password(s) for user {UserId}.", passwordArray.Length, ctx.UserId);

        var passwordList = await GetOrLoadCacheAsync(ctx);

        foreach (var newItem in passwordArray)
            newItem.UserName = ctx.UserId;

        passwordList.AddRange(passwordArray);

        await FlushCacheToDiskAsync(ctx);
        logger.LogInformation("Successfully created {Count} password(s).", passwordArray.Length);
    }

    /// <summary>
    /// Updates an existing password record with new values and persists the changes.
    /// </summary>
    public async Task<PangoPassword> UpdateAsync(PangoPassword password, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
            throw new ArgumentException($"Invalid context type. Expected {typeof(FileRepositoryActionContext).FullName}.", nameof(context));

        logger.LogDebug("Updating password with ID {PasswordId} for user {UserId}.", password.Id, ctx.UserId);

        var passwordList = await GetOrLoadCacheAsync(ctx);
        var pwdToUpdate = passwordList.FirstOrDefault(p => p.Id == password.Id)
                          ?? throw new PasswordNotFoundException($"Password with ID \"{password.Id}\" not found.");

        pwdToUpdate.Name = password.Name;
        pwdToUpdate.Login = password.Login;
        pwdToUpdate.Properties = password.Properties;
        pwdToUpdate.Value = password.Value;
        pwdToUpdate.Target = password.Target;
        pwdToUpdate.CatalogPath = password.CatalogPath;
        pwdToUpdate.Star = password.Star;
        pwdToUpdate.IsCatalog = password.IsCatalog;
        pwdToUpdate.LastModifiedAt = DateTimeOffset.UtcNow;

        await FlushCacheToDiskAsync(ctx);
        logger.LogInformation("Password {PasswordId} updated successfully.", password.Id);
        return pwdToUpdate;
    }

    /// <summary>
    /// Updates multiple password records and persists the changes exactly ONCE.
    /// </summary>
    public async Task UpdateAsync(IEnumerable<PangoPassword> passwords, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
            throw new ArgumentException($"Invalid context type.", nameof(context));

        var pwdArray = passwords.ToArray();
        if (pwdArray.Length == 0) return;

        logger.LogDebug("Batch updating {Count} passwords for user {UserId}.", pwdArray.Length, ctx.UserId);

        var passwordList = await GetOrLoadCacheAsync(ctx);
        bool hasChanges = false;

        foreach (var password in pwdArray)
        {
            var pwdToUpdate = passwordList.FirstOrDefault(p => p.Id == password.Id);
            if (pwdToUpdate != null)
            {
                pwdToUpdate.Name = password.Name;
                pwdToUpdate.Login = password.Login;
                pwdToUpdate.Properties = password.Properties;
                pwdToUpdate.Value = password.Value;
                pwdToUpdate.Target = password.Target;
                pwdToUpdate.CatalogPath = password.CatalogPath;
                pwdToUpdate.Star = password.Star;
                pwdToUpdate.IsCatalog = password.IsCatalog;
                pwdToUpdate.LastModifiedAt = DateTimeOffset.UtcNow;
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await FlushCacheToDiskAsync(ctx);
            logger.LogInformation("Successfully batch updated {Count} passwords.", pwdArray.Length);
        }
    }

    /// <summary>
    /// Wipes the decrypted RAM cache securely.
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            if (_sessionCache != null)
            {
                foreach (var pwd in _sessionCache)
                {
                    pwd.Dispose();
                }
                _sessionCache.Clear();
                _sessionCache = null;
            }
            _cachedUserId = null;
        }
        logger.LogInformation("Password RAM cache wiped securely.");
    }

    /// <summary>
    /// Deletes a password record by its ID and persists the removal.
    /// </summary>
    public async Task DeleteAsync(PangoPassword password, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
            throw new ArgumentException($"Invalid context type. Expected {typeof(FileRepositoryActionContext).FullName}.", nameof(context));

        logger.LogDebug("Attempting to delete password with ID {PasswordId} for user {UserId}.", password.Id, ctx.UserId);

        var passwordList = await GetOrLoadCacheAsync(ctx);
        var pwdToRemove = passwordList.FirstOrDefault(p => p.Id == password.Id);

        if (pwdToRemove != null)
        {
            passwordList.Remove(pwdToRemove);

            pwdToRemove.Dispose();

            await FlushCacheToDiskAsync(ctx);
            logger.LogInformation("Password {PasswordId} deleted successfully.", password.Id);
        }
        else
        {
            logger.LogWarning("Password with ID {PasswordId} not found for deletion.", password.Id);
        }
    }

    /// <summary>
    /// Finds a single password matching the specified predicate.
    /// </summary>
    public async Task<PangoPassword?> FindAsync(Func<PangoPassword, bool> predicate, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
            throw new ArgumentException($"Invalid context type. Expected {typeof(FileRepositoryActionContext).FullName}.", nameof(context));

        logger.LogTrace("Searching for a password by predicate for user {UserId}.", ctx.UserId);

        var items = await GetOrLoadCacheAsync(ctx);
        var result = items.FirstOrDefault(predicate);

        if (result != null)
            logger.LogTrace("Found password with ID {PasswordId}.", result.Id);

        return result;
    }

    /// <summary>
    /// Queries multiple passwords matching the specified predicate.
    /// </summary>
    public async Task<IEnumerable<PangoPassword>> QueryAsync(Func<PangoPassword, bool> predicate, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
            throw new ArgumentException($"Invalid context type. Expected {typeof(FileRepositoryActionContext).FullName}.", nameof(context));

        logger.LogTrace("Querying passwords by predicate for user {UserId}.", ctx.UserId);

        var items = await GetOrLoadCacheAsync(ctx);
        return items.Where(predicate);
    }
}
