using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Queries.UserPasswords;

/// <summary>
/// Handles the query to retrieve all passwords for the current user.
/// </summary>
public class UserPasswordsQueryHandler(
    IPasswordRepository passwordRepository,
    IUserContextProvider userContextProvider,
    IRepositoryContextFactory repositoryContextFactory,
    ILogger<UserPasswordsQueryHandler> logger) : IRequestHandler<UserPasswordsQuery, ErrorOr<IEnumerable<PangoPasswordListItemDto>>>
{
    private readonly IPasswordRepository _passwordRepository = passwordRepository;
    private readonly IUserContextProvider _userContextProvider = userContextProvider;
    private readonly IRepositoryContextFactory _repositoryContextFactory = repositoryContextFactory;
    private readonly ILogger<UserPasswordsQueryHandler> _logger = logger;

    public async Task<ErrorOr<IEnumerable<PangoPasswordListItemDto>>> Handle(UserPasswordsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var encodingOptions = await _userContextProvider.GetEncodingOptionsAsync();
            var context = _repositoryContextFactory.Create(_userContextProvider.GetUserName(), encodingOptions);
            var rawPasswords = await _passwordRepository.QueryAsync(p => true, context);
            var result = await Task.Run(() =>
            {
                return rawPasswords.Select(p => p.Adapt<PangoPasswordListItemDto>()).ToList();
            }, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query of passwords failed: {Message}", ex.Message);
            return Error.Failure(ApplicationErrors.Password.QueryFailed, "Query of passwords failed");
        }
    }
}
