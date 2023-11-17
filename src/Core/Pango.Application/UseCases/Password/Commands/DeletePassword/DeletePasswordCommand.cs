using ErrorOr;
using MediatR;

namespace Pango.Application.UseCases.Password.Commands.DeletePassword;

public class DeletePasswordCommand : IRequest<ErrorOr<bool>>
{
	public DeletePasswordCommand(Guid passwordId)
	{
		PasswordId = passwordId;
	}

	public Guid PasswordId { get; set; }
}
