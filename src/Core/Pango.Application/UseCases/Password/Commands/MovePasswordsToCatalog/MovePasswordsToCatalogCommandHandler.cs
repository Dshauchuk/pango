using ErrorOr;
using MediatR;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.Password.Commands.MovePasswordsToCatalog;

public class MovePasswordsToCatalogCommandHandler
    : IRequestHandler<MovePasswordsToCatalogCommand, ErrorOr<bool>>
{
    private readonly IPasswordRepository _passwordRepository;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IRepositoryContextFactory _repositoryContextFactory;

    public MovePasswordsToCatalogCommandHandler(
        IPasswordRepository passwordRepository,
        IUserContextProvider userContextProvider,
        IRepositoryContextFactory repositoryContextFactory)
    {
        _passwordRepository = passwordRepository;
        _userContextProvider = userContextProvider;
        _repositoryContextFactory = repositoryContextFactory;
    }

    public async Task<ErrorOr<bool>> Handle(MovePasswordsToCatalogCommand request, CancellationToken cancellationToken)
    {
        if (request.PasswordIdCatalogPathPairs.Count == 0)
        {
            return true;
        }

        IRepositoryActionContext context = _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync());

        IEnumerable<PangoPassword> passwords = await _passwordRepository.QueryAsync(p => request.PasswordIdCatalogPathPairs.ContainsKey(p.Id), context);

        if (passwords.Count() != request.PasswordIdCatalogPathPairs.Keys.Count)
        {
            IEnumerable<Guid> missingPasswords = request.PasswordIdCatalogPathPairs.Keys.Except(passwords.Select(p => p.Id));
            throw new PasswordNotFoundException($"Passwords with ids {string.Join(", ", missingPasswords.Select(mp => mp.ToString()))} not found");
        }

        // TODO: create update(many)
        foreach (PangoPassword passwordToUpdate in passwords)
        {
            passwordToUpdate.CatalogPath = request.PasswordIdCatalogPathPairs[passwordToUpdate.Id];
            await _passwordRepository.UpdateAsync(passwordToUpdate, context);
        }

        return true;
    }
}
