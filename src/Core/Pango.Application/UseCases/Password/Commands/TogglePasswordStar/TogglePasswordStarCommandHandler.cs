using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;

namespace Pango.Application.UseCases.Password.Commands.ToggleStar;

public class TogglePasswordStarCommandHandler
    : IRequestHandler<TogglePasswordStarCommand, ErrorOr<bool>>
{
    private readonly IPasswordRepository _passwordRepository;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IRepositoryContextFactory _repositoryContextFactory;
    private readonly ILogger<TogglePasswordStarCommandHandler> _logger;

    public TogglePasswordStarCommandHandler(
        IPasswordRepository passwordRepository,
        IUserContextProvider userContextProvider,
        IRepositoryContextFactory repositoryContextFactory,
        ILogger<TogglePasswordStarCommandHandler> logger)
    {
        _passwordRepository = passwordRepository;
        _userContextProvider = userContextProvider;
        _repositoryContextFactory = repositoryContextFactory;
        _logger = logger;
    }

    public async Task<ErrorOr<bool>> Handle(
        TogglePasswordStarCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var context = _repositoryContextFactory.Create(
                _userContextProvider.GetUserName(),
                await _userContextProvider.GetEncodingOptionsAsync());

            var password = await _passwordRepository.FindAsync(
                p => p.Id == request.PasswordId, context);

            if (password is null)
            {
                return Error.Failure(
                    ApplicationErrors.Password.NotFound,
                    $"Password with id {request.PasswordId} not found");
            }

            password.Star = request.Star;
            await _passwordRepository.UpdateAsync(password, context);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle star for {Id}: {Message}",
                request.PasswordId, ex.Message);
            return Error.Failure(
                ApplicationErrors.Password.ModificationFailed, ex.Message);
        }
    }
}