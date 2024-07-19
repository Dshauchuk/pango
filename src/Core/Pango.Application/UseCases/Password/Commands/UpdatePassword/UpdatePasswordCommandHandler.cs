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
    private readonly ILogger<UpdatePasswordCommandHandler> _logger;

    public UpdatePasswordCommandHandler(IPasswordRepository passwordRepository, IUserContextProvider userContextProvider, ILogger<UpdatePasswordCommandHandler> logger)
    {
        _passwordRepository = passwordRepository;
        _userContextProvider = userContextProvider;
        _logger = logger;
    }

    public async Task<ErrorOr<PangoPasswordDto>> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            PangoPassword? password = await _passwordRepository.FindAsync(_userContextProvider.GetUserName(), p => p.Id == request.PasswordId);

            if (password is null)
            {
                return Error.Failure(ApplicationErrors.Password.NotFound, $"Password with id {request.PasswordId} cannot be deleted: password not found");
            }

            if (password.IsCatalog)
            {
                string oldCatalogPath = request.CatalogPath + (string.IsNullOrEmpty(request.CatalogPath) ? string.Empty : AppConstants.CatalogDelimeter) + password.Name;
                var catalogPasswords = await _passwordRepository.QueryAsync(_userContextProvider.GetUserName(), p => p.CatalogPath == oldCatalogPath);

                if (catalogPasswords.Any())
                {
                    string newCatalogPath = request.CatalogPath + (string.IsNullOrEmpty(request.CatalogPath) ? string.Empty : AppConstants.CatalogDelimeter) + request.Name;

                    // DS
                    // TODO: create update(many)
                    foreach (var pwd in catalogPasswords)
                    {
                        pwd.CatalogPath = newCatalogPath;
                        await _passwordRepository.UpdateAsync(pwd);
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

            PangoPassword updated = await _passwordRepository.UpdateAsync(password);

            return updated.Adapt<PangoPasswordDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password with id {PasswordId} cannot be modified: {Message}", request.PasswordId, ex.Message);
            return Error.Failure(ApplicationErrors.Password.ModificationFailed, $"Password with id {request.PasswordId} cannot be modified: {ex.Message}");
        }
    }
}
