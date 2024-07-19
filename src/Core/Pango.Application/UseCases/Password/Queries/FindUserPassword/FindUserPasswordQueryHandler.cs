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
    : IRequestHandler<FindUserPasswordQuery, ErrorOr<Models.PangoPasswordDto>>
{
    private readonly IPasswordRepository _passwordRepository;
    private readonly IUserContextProvider _userContextProvider;
    private readonly ILogger<FindUserPasswordQueryHandler> _logger;

    public FindUserPasswordQueryHandler(IPasswordRepository passwordRepository, IUserContextProvider userContextProvider, ILogger<FindUserPasswordQueryHandler> logger)
    {
        _passwordRepository = passwordRepository;
        _userContextProvider = userContextProvider;
        _logger = logger;
    }

    public async Task<ErrorOr<PangoPasswordDto>> Handle(FindUserPasswordQuery request, CancellationToken cancellationToken)
    {
        try
        {
            PangoPassword? password = await _passwordRepository.FindAsync(_userContextProvider.GetUserName(), pwd => pwd.Id == request.PasswordId);

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
