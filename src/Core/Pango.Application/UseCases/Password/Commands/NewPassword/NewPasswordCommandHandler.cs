using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Commands.NewPassword;

public class NewPasswordCommandHandler
    : IRequestHandler<NewPasswordCommand, ErrorOr<PangoPasswordDto>>
{
    private readonly IPasswordRepository _passwordRepository;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IRepositoryContextFactory _repositoryContextFactory;
    private readonly ILogger<NewPasswordCommandHandler> _logger;

    public NewPasswordCommandHandler(
        IPasswordRepository passwordRepository,
        IUserContextProvider userContextProvider,
        IRepositoryContextFactory repositoryContextFactory,
        ILogger<NewPasswordCommandHandler> logger)
    {
        _passwordRepository = passwordRepository;
        _userContextProvider = userContextProvider;
        _repositoryContextFactory = repositoryContextFactory;
        _logger = logger;
    }

    public async Task<ErrorOr<PangoPasswordDto>> Handle(NewPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            Domain.Entities.PangoPassword entity = new()
            {
                Login = request.Login,
                Name = request.Name,
                Properties = request.Properties,
                UserName = _userContextProvider.GetUserName(),
                Value = request.Value,
                CreatedAt = DateTimeOffset.UtcNow,
                CatalogPath = request.CatalogPath,
                IsCatalog = request.IsCatalogHolder,
            };

            await _passwordRepository.CreateAsync(entity, _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync()));

            return entity.Adapt<PangoPasswordDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password {PasswordName} cannot be created: {Message}", request.Name, ex.Message);
            return Error.Failure(ApplicationErrors.Password.CreationFailed, $"Password {request.Name} cannot be created: {ex.Message}");
        }
    }
}
