using Microsoft.Extensions.Logging;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;

namespace Pango.Persistence;

public class PasswordRepository : FileRepositoryBase<PangoPassword>, IPasswordRepository
{
    protected override string DirectoryName => "Passwords";

    public PasswordRepository(IContentEncoder contentEncoder, 
        IAppDomainProvider appDomainProvider, 
        IUserContextProvider userContextProvider,
        ILogger logger,
        IAppOptions appOptions)
        : base(contentEncoder, appDomainProvider, appOptions, userContextProvider, logger)
    {
    }

    public async Task CreateAsync(PangoPassword password)
    {
        string userId = UserContextProvider.GetUserName();
        password.UserName = userId;

        var passwordList = (await ExtractAllItemsForUserAsync()).ToList();
        passwordList.Add(password);

        await SaveItemsForUserAsync(passwordList);
    }

    public async Task<PangoPassword> UpdateAsync(PangoPassword password)
    {
        var passwordList = (await ExtractAllItemsForUserAsync()).ToList();

        var pwdToUpdate = passwordList.FirstOrDefault(p => p.Id == password.Id);

        if(pwdToUpdate is null)
        {
            throw new PasswordNotFoundException($"Pango password with ID \"{password.Id}\" not found");
        }

        pwdToUpdate.Name = password.Name;
        pwdToUpdate.Login = password.Login;
        pwdToUpdate.Properties = password.Properties;
        pwdToUpdate.Value = password.Value;
        pwdToUpdate.Target = password.Target;
        pwdToUpdate.LastModifiedAt = DateTimeOffset.UtcNow;

        await SaveItemsForUserAsync(passwordList);

        return pwdToUpdate;
    }

    public async Task DeleteAsync(PangoPassword password)
    {
        var passwordList = (await ExtractAllItemsForUserAsync()).ToList();

        var pwdToRemove = passwordList.FirstOrDefault(p => p.Id == password.Id);

        if (pwdToRemove != null) 
        {
            passwordList.Remove(pwdToRemove);
            await SaveItemsForUserAsync(passwordList);
        }
    }

    public async Task<PangoPassword> FindAsync(Func<PangoPassword, bool> predicate)
    {
        return (await ExtractAllItemsForUserAsync()).FirstOrDefault(predicate);
    }

    public async Task<IEnumerable<PangoPassword>> QueryAsync(Func<PangoPassword, bool> predicate)
    {
        return await ExtractAllItemsForUserAsync();
    }
}
