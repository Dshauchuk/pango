using ErrorOr;
using MediatR;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.Password.Commands.UpdatePasswordsCatalogPath;

public class UpdatePasswordsCatalogPathCommandHandler
    : IRequestHandler<UpdatePasswordsCatalogPathCommand, ErrorOr<bool>>
{
    private readonly IPasswordRepository _passwordRepository;

    public UpdatePasswordsCatalogPathCommandHandler(IPasswordRepository passwordRepository)
    {
        _passwordRepository = passwordRepository;
    }

    public async Task<ErrorOr<bool>> Handle(UpdatePasswordsCatalogPathCommand request, CancellationToken cancellationToken)
    {
        if (request.PasswordIdCatalogPathPairs.Count == 0)
        {
            return true;
        }

        IEnumerable<PangoPassword> passwords = await _passwordRepository.QueryAsync(p => request.PasswordIdCatalogPathPairs.ContainsKey(p.Id));

        if (passwords.Count() != request.PasswordIdCatalogPathPairs.Keys.Count)
        {
            IEnumerable<Guid> missingPasswords = request.PasswordIdCatalogPathPairs.Keys.Except(passwords.Select(p => p.Id));
            throw new PasswordNotFoundException($"Passwords with ids {string.Join(", ", missingPasswords.Select(mp => mp.ToString()))} not found");
        }

        // TODO: create update(many)
        foreach (PangoPassword passwordToUpdate in passwords)
        {
            passwordToUpdate.CatalogPath = request.PasswordIdCatalogPathPairs[passwordToUpdate.Id];
            await _passwordRepository.UpdateAsync(passwordToUpdate);
        }

        return true;
    }
}
