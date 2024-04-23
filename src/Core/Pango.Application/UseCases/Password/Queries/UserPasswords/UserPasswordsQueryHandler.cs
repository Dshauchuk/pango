using ErrorOr;
using Mapster;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Queries.UserPasswords;

public class UserPasswordsQueryHandler
    : IRequestHandler<UserPasswordsQuery, ErrorOr<IEnumerable<Models.PangoPasswordListItemDto>>>
{
    private readonly IPasswordRepository _passwordRepository;

    public UserPasswordsQueryHandler(IPasswordRepository passwordRepository)
    {
        _passwordRepository = passwordRepository;
    }

    public async Task<ErrorOr<IEnumerable<Models.PangoPasswordListItemDto>>> Handle(UserPasswordsQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<PangoPasswordListItemDto> passwords = (await _passwordRepository.QueryAsync(p => true)).Select(p => p.Adapt<PangoPasswordListItemDto>());
        //IEnumerable<PangoPasswordListItemDto> passwords = new List<PangoPasswordListItemDto>()
        //{
        //    new PangoPasswordListItemDto()
        //    {
        //        Id = Guid.NewGuid(),
        //        IsCatalog = true,
        //        Children = new List<PangoPasswordListItemDto>()
        //        {
        //            new PangoPasswordListItemDto()
        //            {
        //                 Id = Guid.NewGuid(),
        //                 IsCatalog = false,
        //                 Name = "My Facebook"
        //            },
        //            new PangoPasswordListItemDto()
        //            {
        //                 Id = Guid.NewGuid(),
        //                 IsCatalog = false,
        //                 Name = "Dad's Facebook"
        //            }
        //        },
        //        Name = "Facebook"
        //    }
        //};        

        return passwords.ToList();
    }
}
