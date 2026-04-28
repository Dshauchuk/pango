using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.Password.Commands.MovePasswordsToCatalog;

public class MovePasswordsToCatalogCommandHandler(
    IPasswordRepository passwordRepository,
    IUserContextProvider userContextProvider,
    IRepositoryContextFactory repositoryContextFactory,
    ILogger<MovePasswordsToCatalogCommandHandler> logger)
        : IRequestHandler<MovePasswordsToCatalogCommand, ErrorOr<bool>>
{
    private readonly IPasswordRepository _passwordRepository = passwordRepository;
    private readonly IUserContextProvider _userContextProvider = userContextProvider;
    private readonly IRepositoryContextFactory _repositoryContextFactory = repositoryContextFactory;
    private readonly ILogger<MovePasswordsToCatalogCommandHandler> _logger = logger;

    public async Task<ErrorOr<bool>> Handle(MovePasswordsToCatalogCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.PasswordIdCatalogPathPairs.Count == 0) return true;

            IRepositoryActionContext context = _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync());
            IEnumerable<PangoPassword> passwords = await _passwordRepository.QueryAsync(p => request.PasswordIdCatalogPathPairs.ContainsKey(p.Id), context);

            var passwordList = passwords.ToList();
            if (passwordList.Count != request.PasswordIdCatalogPathPairs.Keys.Count)
            {
                var missingIds = string.Join(", ", request.PasswordIdCatalogPathPairs.Keys.Except(passwordList.Select(p => p.Id)));
                return Error.Failure(ApplicationErrors.Password.NotFound, $"Some passwords to move were not found: {missingIds}");
            }

            foreach (PangoPassword passwordToUpdate in passwordList)
            {
                passwordToUpdate.CatalogPath = request.PasswordIdCatalogPathPairs[passwordToUpdate.Id];
            }

            await _passwordRepository.UpdateAsync(passwordList, context);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Move failed");
            return Error.Failure(ApplicationErrors.Password.ModificationFailed, ex.Message);
        }
    }
}
