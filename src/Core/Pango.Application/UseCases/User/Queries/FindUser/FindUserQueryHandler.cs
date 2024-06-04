using ErrorOr;
using Mapster;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.User.Queries.FindUser;

public class FindUserQueryHandler
    : IRequestHandler<FindUserQuery, ErrorOr<PangoUserDto>>
{
    private readonly IUserRepository _userRepository;

    public FindUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ErrorOr<PangoUserDto>> Handle(FindUserQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request?.Name))
        {
            return Error.Validation();
        }

        PangoUser? user = await _userRepository.FindAsync(request!.Name!);

        if (user is null)
        {
            return Error.NotFound();
        }

        return user.Adapt<PangoUserDto>();
    }
}
