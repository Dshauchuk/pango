using ErrorOr;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;

namespace Pango.Application.UseCases.User.Queries.List;

public class ListQueryHandler
    : IRequestHandler<ListQuery, ErrorOr<IEnumerable<Models.UserDto>>>
{
    private readonly IUserRepository _userRepository;

    public ListQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ErrorOr<IEnumerable<Models.UserDto>>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<Models.UserDto> users = (await _userRepository.ListAsync()).Select(u => new UserDto() { UserName = u.UserName, MasterPasswordHash = u.MasterPasswordHash });
        return users.ToList();
    }
}
