using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Queries.UserPasswords;

public class UserPasswordsQueryHandler
    : IRequestHandler<UserPasswordsQuery, ErrorOr<IEnumerable<Models.PangoPasswordListItemDto>>>
{
    private readonly IPasswordRepository _passwordRepository;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IRepositoryContextFactory _repositoryContextFactory;
    private readonly ILogger<UserPasswordsQueryHandler> _logger;

    public UserPasswordsQueryHandler(
        IPasswordRepository passwordRepository, 
        IUserContextProvider userContextProvider, 
        IRepositoryContextFactory repositoryContextFactory,
        ILogger<UserPasswordsQueryHandler> logger)
    {
        _passwordRepository = passwordRepository;
        _userContextProvider = userContextProvider;
        _repositoryContextFactory = repositoryContextFactory;
        _logger = logger;
    }

    public async Task<ErrorOr<IEnumerable<PangoPasswordListItemDto>>> Handle(UserPasswordsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<PangoPasswordListItemDto> passwords = 
                (await _passwordRepository.QueryAsync(p => true, _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync())))
                .Select(p => p.Adapt<PangoPasswordListItemDto>());

            return passwords.ToList();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Query of passwords failed: {Message}", ex.Message);
            return Error.Failure(ApplicationErrors.Password.QueryFailed, "Query of passwords failed");
        }
    }
}
