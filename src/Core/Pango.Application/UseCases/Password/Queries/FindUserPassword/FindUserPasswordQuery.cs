using ErrorOr;
using MediatR;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Queries.FindUserPassword;

public record FindUserPasswordQuery() : IRequest<ErrorOr<PangoPasswordDto>>
{
	public FindUserPasswordQuery(Guid passwordId) : this()
	{
		PasswordId = passwordId;
	}

    public Guid PasswordId { get; }
}

