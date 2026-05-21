using ErrorOr;
using MediatR;

namespace Pango.Application.UseCases.Password.Commands.ToggleStar;

public record TogglePasswordStarCommand(Guid PasswordId, bool Star)
    : IRequest<ErrorOr<bool>>;