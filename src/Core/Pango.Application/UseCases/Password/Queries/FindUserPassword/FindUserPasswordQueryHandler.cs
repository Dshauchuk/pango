using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.Password.Queries.FindUserPassword;

public class FindUserPasswordQueryHandler
    : IRequestHandler<FindUserPasswordQuery, ErrorOr<PangoPasswordDto>>
{
    private readonly IPasswordRepository _passwordRepository;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IRepositoryContextFactory _repositoryContextFactory;
    private readonly ILogger<FindUserPasswordQueryHandler> _logger;

    public FindUserPasswordQueryHandler(
        IPasswordRepository passwordRepository, 
        IUserContextProvider userContextProvider, 
        IRepositoryContextFactory repositoryContextFactory,
        ILogger<FindUserPasswordQueryHandler> logger)
    {
        _passwordRepository = passwordRepository;
        _userContextProvider = userContextProvider;
        _logger = logger;
        _repositoryContextFactory = repositoryContextFactory;
    }

    public async Task<ErrorOr<PangoPasswordDto>> Handle(FindUserPasswordQuery request, CancellationToken cancellationToken)
    {
        try
        {
            PangoPassword? password = await _passwordRepository.FindAsync(pwd => pwd.Id == request.PasswordId, _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync()));

            if (password is null)
            {
                return Error.NotFound(ApplicationErrors.Password.NotFound, $"Password with id {request.PasswordId} not found");
            }

            return password.Adapt<PangoPasswordDto>();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Password with id {PasswordId} not found", request.PasswordId);
            return Error.Failure(ApplicationErrors.Password.NotFound, $"Password with id {request.PasswordId} not found");
        }
    }
}
