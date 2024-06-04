using ErrorOr;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.Password.Commands.DeleteUserPasswords;

public class DeleteUserPasswordsCommandHandler
    : IRequestHandler<DeleteUserPasswordsCommand, ErrorOr<bool>>
{
    private readonly IPasswordRepository _passwordRepository;

    public DeleteUserPasswordsCommandHandler(IPasswordRepository passwordRepository)
    {
        _passwordRepository = passwordRepository;
    }

    public async Task<ErrorOr<bool>> Handle(DeleteUserPasswordsCommand request, CancellationToken cancellationToken)
    {
        IEnumerable<PangoPassword> userPasswords = await _passwordRepository.QueryAsync(p => true);

        if (userPasswords?.Any() == true)
        {
            foreach (PangoPassword password in userPasswords)
            {
                await _passwordRepository.DeleteAsync(password);
            }

            return true;
        }

        return false;
    }
}
