using ErrorOr;
using MediatR;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.Password.Commands.DeletePassword;

public class DeletePasswordCommandHandler
    : IRequestHandler<DeletePasswordCommand, ErrorOr<bool>>
{
    private readonly IPasswordRepository _passwordRepository;

    public DeletePasswordCommandHandler(IPasswordRepository passwordRepository)
    {
        _passwordRepository = passwordRepository;
    }

    public async Task<ErrorOr<bool>> Handle(DeletePasswordCommand request, CancellationToken cancellationToken)
    {
        var password = await _passwordRepository.FindAsync(pwd => pwd.Id == request.PasswordId);
    
        if(password is not null)
        {
            List<PangoPassword> passwordsToRemove = new() { password };

            if (password.IsCatalog)
            {
                string catalogPath = string.IsNullOrEmpty(password.CatalogPath) ? password.Name : $"{password.CatalogPath}{AppConstants.CatalogDelimeter}{password.Name}";
                passwordsToRemove = (await _passwordRepository.QueryAsync(p => p.CatalogPath == catalogPath)).ToList();
            }

            foreach (var pwd in passwordsToRemove)
            {
                await _passwordRepository.DeleteAsync(pwd);
            }

            return true;
        }

        return false;
    }
}
