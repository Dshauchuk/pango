using ErrorOr;
using MediatR;
using Pango.Application.Models;

public record ListQuery() : IRequest<ErrorOr<IEnumerable<PangoUserDto>>>;