using ErrorOr;
using Mapster;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Queries.FindUserPassword;

public class FindUserPasswordQueryHandler
    : IRequestHandler<FindUserPasswordQuery, ErrorOr<Models.PangoPasswordDto>>
{
    private readonly IPasswordRepository _passwordRepository;

    public FindUserPasswordQueryHandler(IPasswordRepository passwordRepository)
    {
        _passwordRepository = passwordRepository;
    }

    public async Task<ErrorOr<PangoPasswordDto>> Handle(FindUserPasswordQuery request, CancellationToken cancellationToken)
    {
        Domain.Entities.PangoPassword password = await _passwordRepository.FindAsync(pwd => pwd.Id == request.PasswordId);

        if(password is null)
        {
            return Error.NotFound();
        }

        return password.Adapt<PangoPasswordDto>();
    }
}
