using ErrorOr;
using MediatR;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Queries.UserPasswords;

public record UserPasswordsQuery() : IRequest<ErrorOr<IEnumerable<PangoPasswordListItemDto>>>;
