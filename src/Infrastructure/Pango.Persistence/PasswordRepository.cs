using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Persistence;

public class PasswordRepository : FileRepositoryBase<PangoPassword>, IPasswordRepository
{
    private readonly IRepositoryContext _context;

    protected override string FileName => "pwddata.dat";

    public PasswordRepository(IContentEncoder contentEncoder, IRepositoryContext context, IAppDomainProvider appDomainProvider)
        : base(contentEncoder, appDomainProvider)
    {
        _context = context;
    }

    public async Task CreateAsync(PangoPassword password)
    {
        string userId = _context.UserId;
        password.UserName = userId;

        var passwordList = (await ExtractAllItemsForUserAsync(userId)).ToList();
        passwordList.Add(password);

        await SaveItemsForUserAsync(userId, passwordList);
    }

    public async Task<PangoPassword> UpdateAsync(PangoPassword password)
    {
        string userId = _context.UserId;
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

        await SaveItemsForUserAsync(_context.UserId, passwordList);

        return pwdToUpdate;
    }

    public async Task DeleteAsync(PangoPassword password)
    {
        string userId = _context.UserId;
        var passwordList = (await ExtractAllItemsForUserAsync(userId)).ToList();

        var pwdToRemove = passwordList.FirstOrDefault(p => p.Id == password.Id);

        if (pwdToRemove != null) 
        {
            passwordList.Remove(pwdToRemove);
            await SaveItemsForUserAsync(_context.UserId, passwordList);
        }
    }

    public async Task<PangoPassword> FindAsync(Func<PangoPassword, bool> predicate)
    {
        string userId = _context.UserId;

        return (await ExtractAllItemsForUserAsync(userId)).FirstOrDefault(predicate);
    }

    public async Task<IEnumerable<PangoPassword>> QueryAsync(Func<PangoPassword, bool> predicate)
    {
        string userId = _context.UserId;

        return await ExtractAllItemsForUserAsync(userId);
    }
}
