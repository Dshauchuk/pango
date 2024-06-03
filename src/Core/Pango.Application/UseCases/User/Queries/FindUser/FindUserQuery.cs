using ErrorOr;
using MediatR;
using Pango.Application.Models;

namespace Pango.Application.UseCases.User.Queries.FindUser;

public record FindUserQuery() : IRequest<ErrorOr<PangoUserDto>>
{
    public FindUserQuery(string name) : this()
    {
        Name = name;
    }

    public string? Name { get; set; }
}
