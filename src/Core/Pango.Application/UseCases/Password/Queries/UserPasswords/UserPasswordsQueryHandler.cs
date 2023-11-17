using ErrorOr;
using Mapster;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Queries.UserPasswords;

public class UserPasswordsQueryHandler
    : IRequestHandler<UserPasswordsQuery, ErrorOr<IEnumerable<Models.PasswordListItemDto>>>
{
    private readonly IPasswordRepository _passwordRepository;

    public UserPasswordsQueryHandler(IPasswordRepository passwordRepository)
    {
        _passwordRepository = passwordRepository;
    }

    public async Task<ErrorOr<IEnumerable<Models.PasswordListItemDto>>> Handle(UserPasswordsQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<PasswordListItemDto> passwords = (await _passwordRepository.QueryAsync(p => true)).Select(p => p.Adapt<PasswordListItemDto>());

        return passwords.ToList();
    }
}
