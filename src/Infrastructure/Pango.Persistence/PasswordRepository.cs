using Microsoft.Extensions.Logging;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Persistence;

public class PasswordRepository : FileRepositoryBase<PangoPassword>, IPasswordRepository
{
    protected override string DirectoryName => "passwords";
    
    public PasswordRepository(IContentEncoder contentEncoder,
        IAppDomainProvider appDomainProvider,
        ILogger<PasswordRepository> logger,
        IAppOptions appOptions)
        : base(contentEncoder, appDomainProvider, appOptions, logger)
    {

    }

    public async Task CreateAsync(PangoPassword password)
    {
        var passwordList = (await ExtractAllItemsForUserAsync(password.UserName)).ToList();
        passwordList.Add(password);

        await SaveItemsForUserAsync(passwordList, password.UserName);
    }

    public async Task<PangoPassword> UpdateAsync(PangoPassword password)
    {
        var passwordList = (await ExtractAllItemsForUserAsync(password.UserName)).ToList();
        
        var pwdToUpdate = passwordList.FirstOrDefault(p => p.Id == password.Id) ?? throw new PasswordNotFoundException($"Pango password with ID \"{password.Id}\" not found");
        pwdToUpdate.Name = password.Name;
        pwdToUpdate.Login = password.Login;
        pwdToUpdate.Properties = password.Properties;
        pwdToUpdate.Value = password.Value;
        pwdToUpdate.Target = password.Target;
        pwdToUpdate.CatalogPath = password.CatalogPath;
        pwdToUpdate.LastModifiedAt = DateTimeOffset.UtcNow;

        await SaveItemsForUserAsync(passwordList, password.UserName);

        return pwdToUpdate;
    }

    public async Task DeleteAsync(PangoPassword password)
    {
        var passwordList = (await ExtractAllItemsForUserAsync(password.UserName)).ToList();

        var pwdToRemove = passwordList.FirstOrDefault(p => p.Id == password.Id);

        if (pwdToRemove != null) 
        {
            passwordList.Remove(pwdToRemove);
            await SaveItemsForUserAsync(passwordList, password.UserName);
        }
    }

    public async Task<PangoPassword?> FindAsync(string userName, Func<PangoPassword, bool> predicate)
    {
        return (await ExtractAllItemsForUserAsync(userName)).FirstOrDefault(predicate);
    }

    public async Task<IEnumerable<PangoPassword>> QueryAsync(string userName, Func<PangoPassword, bool> predicate)
    {
        return (await ExtractAllItemsForUserAsync(userName)).Where(predicate);
    }
}
