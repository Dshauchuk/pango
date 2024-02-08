using ErrorOr;
using Mapster;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Commands.NewPassword;

public class NewPasswordCommandHandler
    : IRequestHandler<NewPasswordCommand, ErrorOr<PangoPasswordDto>>
{
    private readonly IPasswordRepository _passwordRepository;

    public NewPasswordCommandHandler(IPasswordRepository passwordRepository)
    {
        _passwordRepository = passwordRepository;
    }

    public async Task<ErrorOr<PangoPasswordDto>> Handle(NewPasswordCommand request, CancellationToken cancellationToken)
    {
        Domain.Entities.PangoPassword entity = new()
        {
            Login = request.Login,
            Name = request.Name,
            Properties = request.Properties,
            Value = request.Value,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _passwordRepository.CreateAsync(entity);

        return entity.Adapt<PangoPasswordDto>();
    }
}
