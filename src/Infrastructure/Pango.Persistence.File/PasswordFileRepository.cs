using Microsoft.Extensions.Logging;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Persistence.File;

public class PasswordFileRepository : FileRepositoryBase<PangoPassword>, IPasswordRepository
{
    protected override string DirectoryName => "passwords";
    
    public PasswordFileRepository(IContentEncoder contentEncoder,
        ILogger<PasswordFileRepository> logger,
        IAppOptions appOptions)
        : base(contentEncoder, appOptions, logger)
    { }

    public async Task CreateAsync(PangoPassword password, IRepositoryActionContext context)
    {
        if(context is not FileRepositoryActionContext ctx)
        {
            throw new ArgumentException($"Invalid type of context. It must be {typeof(FileRepositoryActionContext).FullName}", nameof(context));       
        }

        var passwordList = (await ExtractAllItemsForUserAsync(Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions)).ToList();
        passwordList.Add(password);

        await SaveItemsForUserAsync(passwordList, password.UserName, Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions);
    }

    public async Task<PangoPassword> UpdateAsync(PangoPassword password, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
        {
            throw new ArgumentException($"Invalid type of context. It must be {typeof(FileRepositoryActionContext).FullName}", nameof(context));
        }

        var passwordList = (await ExtractAllItemsForUserAsync(Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions)).ToList();
        
        var pwdToUpdate = passwordList.FirstOrDefault(p => p.Id == password.Id) ?? throw new PasswordNotFoundException($"Pango password with ID \"{password.Id}\" not found");
        pwdToUpdate.Name = password.Name;
        pwdToUpdate.Login = password.Login;
        pwdToUpdate.Properties = password.Properties;
        pwdToUpdate.Value = password.Value;
        pwdToUpdate.Target = password.Target;
        pwdToUpdate.CatalogPath = password.CatalogPath;
        pwdToUpdate.LastModifiedAt = DateTimeOffset.UtcNow;

        await SaveItemsForUserAsync(passwordList, password.UserName, Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions);

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
            await SaveItemsForUserAsync(passwordList, password.UserName, Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions);
        }
    }

    public async Task<PangoPassword?> FindAsync(Func<PangoPassword, bool> predicate, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
        {
            throw new ArgumentException($"Invalid type of context. It must be {typeof(FileRepositoryActionContext).FullName}", nameof(context));
        }

        return (await ExtractAllItemsForUserAsync(Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions)).FirstOrDefault(predicate);
    }

    public async Task<IEnumerable<PangoPassword>> QueryAsync(Func<PangoPassword, bool> predicate, IRepositoryActionContext context)
    {
        if (context is not FileRepositoryActionContext ctx)
        {
            throw new ArgumentException($"Invalid type of context. It must be {typeof(FileRepositoryActionContext).FullName}", nameof(context));
        }

        return (await ExtractAllItemsForUserAsync(Path.Combine(ctx.WorkingDirectoryPath, DirectoryName), ctx.EncodingOptions)).Where(predicate);
    }
}
