using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
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
    private readonly ILogger<MovePasswordsToCatalogCommandHandler> _logger;

    public MovePasswordsToCatalogCommandHandler(
        IPasswordRepository passwordRepository,
        IUserContextProvider userContextProvider,
        IRepositoryContextFactory repositoryContextFactory,
        ILogger<MovePasswordsToCatalogCommandHandler> logger)
    {
        _passwordRepository = passwordRepository;
        _userContextProvider = userContextProvider;
        _repositoryContextFactory = repositoryContextFactory;
        _logger = logger;
    }

    public async Task<ErrorOr<bool>> Handle(MovePasswordsToCatalogCommand request, CancellationToken cancellationToken)
    {
        try
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
                return Error.Failure(ApplicationErrors.Password.NotFound, $"Passwords with ids {string.Join(", ", missingPasswords.Select(mp => mp.ToString()))} cannot be moved to the new catalog: passwords not found");
            }

            // TODO: create update(many)
            foreach (PangoPassword passwordToUpdate in passwords)
            {
                passwordToUpdate.CatalogPath = request.PasswordIdCatalogPathPairs[passwordToUpdate.Id];
                await _passwordRepository.UpdateAsync(passwordToUpdate, context);
            }

            return true;
        }
        catch (Exception ex)
        {
            IEnumerable<string> failedPasswordIdStrings = request.PasswordIdCatalogPathPairs.Keys.Select(g => g.ToString());
            _logger.LogError(ex, "Passwords with ids {PasswordIds} cannot be moved to the new catalog: {Message}", string.Join(", ", failedPasswordIdStrings), ex.Message);
            return Error.Failure(ApplicationErrors.Password.ModificationFailed, $"Passwords with ids {failedPasswordIdStrings} cannot be moved to the new catalog: {ex.Message}");
        }
    }
}
