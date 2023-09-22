using ErrorOr;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;

namespace Pango.Application.UseCases.User.Queries.List;

public class ListQueryHandler
    : IRequestHandler<ListQuery, ErrorOr<IEnumerable<UserDto>>>
{
    private readonly IUserRepository _userRepository;

    public ListQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ErrorOr<IEnumerable<UserDto>>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<UserDto> users = await _userRepository.ListAsync();
        return users.ToList();
    }
}
