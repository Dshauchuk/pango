using ErrorOr;
using Mapster;
using MediatR;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.Password.Commands.UpdatePassword;

public class UpdatePasswordCommandHandler
: IRequestHandler<UpdatePasswordCommand, ErrorOr<PangoPasswordDto>>
{
    private readonly IPasswordRepository _passwordRepository;

    public UpdatePasswordCommandHandler(IPasswordRepository passwordRepository)
    {
        _passwordRepository = passwordRepository;
    }

    public async Task<ErrorOr<PangoPasswordDto>> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
    {
        PangoPassword password = await _passwordRepository.FindAsync(p => p.Id == request.PasswordId) ?? throw new PasswordNotFoundException($"Password with id {request.PasswordId} not found");

        if (password.IsCatalog)
        {
            string oldCatalogPath = request.CatalogPath + (string.IsNullOrEmpty(request.CatalogPath) ? string.Empty : AppConstants.CatalogDelimeter) + password.Name;
            var catalogPasswords = await _passwordRepository.QueryAsync(p => p.CatalogPath == oldCatalogPath);

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
        }

        password.Name = request.Name;
        password.CatalogPath = request.CatalogPath;

        PangoPassword updated = await _passwordRepository.UpdateAsync(password);

        return updated.Adapt<PangoPasswordDto>();
    }
}
