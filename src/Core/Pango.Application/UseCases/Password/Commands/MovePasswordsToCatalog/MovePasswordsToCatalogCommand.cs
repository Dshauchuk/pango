using ErrorOr;
using MediatR;

namespace Pango.Application.UseCases.Password.Commands.MovePasswordsToCatalog;

public class MovePasswordsToCatalogCommand : IRequest<ErrorOr<bool>>
{
    public MovePasswordsToCatalogCommand(Dictionary<Guid, string> passwordIdCatalogPathPairs)
    {
        PasswordIdCatalogPathPairs = passwordIdCatalogPathPairs;
    }

    public Dictionary<Guid, string> PasswordIdCatalogPathPairs { get; }
}
