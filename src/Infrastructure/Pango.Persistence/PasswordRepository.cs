using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Persistence;

public class PasswordRepository : FileRepositoryBase<PangoPassword>, IPasswordRepository
{
    private readonly IAppUserProvider _userProvider;

    protected override string FileName => "pwddata.dat";

    public PasswordRepository(IContentEncoder contentEncoder, IAppUserProvider userProvider, IAppDomainProvider appDomainProvider)
        : base(contentEncoder, appDomainProvider)
    {
        _userProvider = userProvider;
    }

    public async Task CreateAsync(PangoPassword password)
    {
        string userId = _userProvider.GetUserId();
        password.UserName = userId;

        var passwordList = (await ExtractAllItemsForUserAsync(userId)).ToList();
        passwordList.Add(password);

        await SaveItemsForUserAsync(userId, passwordList);
    }

    public async Task<PangoPassword> UpdateAsync(PangoPassword password)
    {
        string userId = _userProvider.GetUserId();
        var passwordList = (await ExtractAllItemsForUserAsync(userId)).ToList();

        var pwdToUpdate = passwordList.FirstOrDefault(p => p.Id == password.Id);

        if(pwdToUpdate is null)
        {
            throw new PasswordNotFoundException($"Pango password with ID \"{password.Id}\" not found");
        }

        pwdToUpdate.Name = password.Name;
        pwdToUpdate.Login= password.Login;
        pwdToUpdate.Properties = password.Properties;
        pwdToUpdate.Value = password.Value;
        pwdToUpdate.Target = password.Target;
        pwdToUpdate.LastModifiedAt = DateTimeOffset.UtcNow;

        await SaveItemsForUserAsync(_userProvider.GetUserId(), passwordList);

        return pwdToUpdate;
    }

    public async Task DeleteAsync(PangoPassword password)
    {
        string userId = _userProvider.GetUserId();
        var passwordList = (await ExtractAllItemsForUserAsync(userId)).ToList();

        var pwdToRemove = passwordList.FirstOrDefault(p => p.Id == password.Id);

        if (pwdToRemove != null) 
        {
            passwordList.Remove(pwdToRemove);
            await SaveItemsForUserAsync(_userProvider.GetUserId(), passwordList);
        }
    }

    public async Task<PangoPassword> FindAsync(Func<PangoPassword, bool> predicate)
    {
        string userId = _userProvider.GetUserId();

        return (await ExtractAllItemsForUserAsync(userId)).FirstOrDefault(predicate);
    }

    public async Task<IEnumerable<PangoPassword>> QueryAsync(Func<PangoPassword, bool> predicate)
    {
        string userId = _userProvider.GetUserId();

        return await ExtractAllItemsForUserAsync(userId);
    }
}
