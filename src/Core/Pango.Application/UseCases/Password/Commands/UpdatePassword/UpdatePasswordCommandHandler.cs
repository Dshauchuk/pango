using ErrorOr;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.Password.Commands.UpdatePassword;

public class UpdatePasswordCommandHandler
: IRequestHandler<UpdatePasswordCommand, ErrorOr<PangoPasswordDto>>
{
    private readonly IPasswordRepository _passwordRepository;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IRepositoryContextFactory _repositoryContextFactory;
    private readonly ILogger<UpdatePasswordCommandHandler> _logger;

    public UpdatePasswordCommandHandler(
        IPasswordRepository passwordRepository, 
        IUserContextProvider userContextProvider,
        IRepositoryContextFactory repositoryContextFactory,
        ILogger<UpdatePasswordCommandHandler> logger)
    {
        _passwordRepository = passwordRepository;
        _userContextProvider = userContextProvider;
        _repositoryContextFactory = repositoryContextFactory;
        _logger = logger;
    }

    public async Task<ErrorOr<PangoPasswordDto>> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            IRepositoryActionContext context = _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync());

            PangoPassword? password = await _passwordRepository.FindAsync(p => p.Id == request.PasswordId, context);

            if (password is null)
            {
                return Error.Failure(ApplicationErrors.Password.NotFound, $"Password with id {request.PasswordId} cannot be deleted: password not found");
            }

            if (password.IsCatalog)
            {
                string oldCatalogPath = request.CatalogPath + (string.IsNullOrEmpty(request.CatalogPath) ? string.Empty : AppConstants.CatalogDelimeter) + password.Name;
                var catalogPasswords = await _passwordRepository.QueryAsync(p => p.CatalogPath == oldCatalogPath, context);

                if (catalogPasswords.Any())
                {
                    string newCatalogPath = request.CatalogPath + (string.IsNullOrEmpty(request.CatalogPath) ? string.Empty : AppConstants.CatalogDelimeter) + request.Name;

                    // DS
                    // TODO: create update(many)
                    foreach (var pwd in catalogPasswords)
                    {
                        pwd.CatalogPath = newCatalogPath;
                        await _passwordRepository.UpdateAsync(pwd, context);
                    }
                }
            }
            else
            {
                password.Value = request.Value;
                password.Login = request.Login;
                password.Properties = request.Properties;
                password.CatalogPath = request.CatalogPath;
            }

            password.Name = request.Name;
            password.CatalogPath = request.CatalogPath;

            PangoPassword updated = await _passwordRepository.UpdateAsync(password, context);

            return updated.Adapt<PangoPasswordDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password with id {PasswordId} cannot be modified: {Message}", request.PasswordId, ex.Message);
            return Error.Failure(ApplicationErrors.Password.ModificationFailed, $"Password with id {request.PasswordId} cannot be modified: {ex.Message}");
        }
    }
}
