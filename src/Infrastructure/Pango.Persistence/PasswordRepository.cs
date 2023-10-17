﻿using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Persistence;

public class PasswordRepository : FileRepositoryBase<Password>, IPasswordRepository
{
    private readonly IRepositoryContext _context;

    protected override string FileName => "pwddata.dat";

    public PasswordRepository(IContentEncoder contentEncoder, IRepositoryContext context)
        : base(contentEncoder)
    {
        _context = context;
    }

    public async Task CreateAsync(Password password)
    {
        string userId = _context.UserId;
        password.UserName = userId;

        var passwordList = (await ExtractAllItemsForUserAsync(userId)).ToList();
        passwordList.Add(password);

        await SaveItemsForUserAsync(userId, passwordList);
    }

    public async Task DeleteAsync(Password password)
    {
        string userId = _context.UserId;
        var passwordList = (await ExtractAllItemsForUserAsync(userId)).ToList();

        if(passwordList.Contains(password)) 
        {
            passwordList.Remove(password);
            await SaveItemsForUserAsync(_context.UserId, passwordList);
        }
    }

    public async Task<Password> FindAsync(Func<Password, bool> predicate)
    {
        string userId = _context.UserId;

        return (await ExtractAllItemsForUserAsync(userId)).FirstOrDefault(predicate);
    }

    public async Task<IEnumerable<Password>> QueryAsync(Func<Password, bool> predicate)
    {
        string userId = _context.UserId;

        return await ExtractAllItemsForUserAsync(userId);
    }
}