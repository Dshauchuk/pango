using Microsoft.Extensions.Logging;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Persistence.File;

public class PasswordFileRepository(
    IContentEncoder contentEncoder,
    IAppDomainProvider appDomainProvider,
    ILogger<PasswordFileRepository> logger,
    IAppOptions appOptions) : FileRepositoryBase<PangoPassword>(contentEncoder, appDomainProvider, appOptions, logger), IPasswordRepository
{
    protected override string DirectoryName => "passwords";

    public async Task CreateAsync(PangoPassword password, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
        {
            throw new ArgumentException($"Invalid type of context. It must be {typeof(FileRepositoryActionContext).FullName}", nameof(context));
        }

        var passwordList = (await ExtractAllItemsForUserAsync(Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions)).ToList();

        bool alreadyExists = passwordList.Any(p =>
            p.Name.Equals(password.Name, StringComparison.OrdinalIgnoreCase) &&
            (p.CatalogPath ?? string.Empty).Equals(password.CatalogPath ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
            p.IsCatalog == password.IsCatalog &&
            (password.IsCatalog || (p.Login ?? string.Empty).Equals(password.Login ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        );

        if (alreadyExists)
        {
            Logger.LogInformation("Item {name} in {path} (IsCatalog: {isCat}) already exists. Skipping.",
                password.Name, password.CatalogPath, password.IsCatalog);
            return;
        }

        password.UserName = ctx.UserId;
        passwordList.Add(password);
        await SaveItemsForUserAsync(passwordList, ctx.UserId, Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions);
    }

    public async Task CreateAsync(IEnumerable<PangoPassword> passwords, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
        {
            throw new ArgumentException($"Invalid type of context. It must be {typeof(FileRepositoryActionContext).FullName}", nameof(context));
        }

        var passwordList = (await ExtractAllItemsForUserAsync(Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions)).ToList();

        foreach (var newItem in passwords)
        {
            bool exists = passwordList.Any(p =>
                p.Name.Equals(newItem.Name, StringComparison.OrdinalIgnoreCase) &&
                (p.CatalogPath ?? string.Empty).Equals(newItem.CatalogPath ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                p.IsCatalog == newItem.IsCatalog &&
                (newItem.IsCatalog || (p.Login ?? string.Empty).Equals(newItem.Login ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            );

            if (!exists)
            {
                newItem.UserName = ctx.UserId;
                passwordList.Add(newItem);
            }
        }
        await SaveItemsForUserAsync(passwordList, ctx.UserId, Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions);
    }

    public async Task<PangoPassword> UpdateAsync(PangoPassword password, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
        {
            throw new ArgumentException($"Invalid type of context. It must be {typeof(FileRepositoryActionContext).FullName}", nameof(context));
        }

        var passwordList = (await ExtractAllItemsForUserAsync(Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions)).ToList();
        var pwdToUpdate = passwordList.FirstOrDefault(p => p.Id == password.Id)
            ?? throw new PasswordNotFoundException($"Pango password with ID \"{password.Id}\" not found");

        pwdToUpdate.Name = password.Name;
        pwdToUpdate.Login = password.Login;
        pwdToUpdate.Properties = password.Properties;
        pwdToUpdate.Value = password.Value;
        pwdToUpdate.Target = password.Target;
        pwdToUpdate.CatalogPath = password.CatalogPath;
        pwdToUpdate.IsCatalog = password.IsCatalog;
        pwdToUpdate.LastModifiedAt = DateTimeOffset.UtcNow;

        await SaveItemsForUserAsync(passwordList, ctx.UserId, Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions);
        return pwdToUpdate;
    }

    public async Task DeleteAsync(PangoPassword password, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
        {
            throw new ArgumentException($"Invalid type of context. It must be {typeof(FileRepositoryActionContext).FullName}", nameof(context));
        }

        var passwordList = (await ExtractAllItemsForUserAsync(Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions)).ToList();
        var pwdToRemove = passwordList.FirstOrDefault(p => p.Id == password.Id);

        if (pwdToRemove != null)
        {
            passwordList.Remove(pwdToRemove);
            await SaveItemsForUserAsync(passwordList, ctx.UserId, Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions);
        }
    }

    public async Task<PangoPassword?> FindAsync(Func<PangoPassword, bool> predicate, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
        {
            throw new ArgumentException($"Invalid type of context. It must be {typeof(FileRepositoryActionContext).FullName}", nameof(context));
        }

        var items = await ExtractAllItemsForUserAsync(Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions);
        return items.FirstOrDefault(predicate);
    }

    public async Task<IEnumerable<PangoPassword>> QueryAsync(Func<PangoPassword, bool> predicate, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
        {
            throw new ArgumentException($"Invalid type of context. It must be {typeof(FileRepositoryActionContext).FullName}", nameof(context));
        }

        var items = await ExtractAllItemsForUserAsync(Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions);
        return items.Where(predicate);
    }
}
