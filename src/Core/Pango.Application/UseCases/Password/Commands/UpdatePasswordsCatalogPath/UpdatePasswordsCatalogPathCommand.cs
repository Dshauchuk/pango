using ErrorOr;
using MediatR;

namespace Pango.Application.UseCases.Password.Commands.UpdatePasswordsCatalogPath;

public class UpdatePasswordsCatalogPathCommand : IRequest<ErrorOr<bool>>
{
    public UpdatePasswordsCatalogPathCommand(Dictionary<Guid, string> passwordIdCatalogPathPairs)
    {
        PasswordIdCatalogPathPairs = passwordIdCatalogPathPairs;
    }

    public Dictionary<Guid, string> PasswordIdCatalogPathPairs { get; }
}
