using ErrorOr;
using Mapster;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Password.Commands;

public class NewPasswordCommandHandler
    : IRequestHandler<NewPasswordCommand, ErrorOr<PasswordDto>>
{
    private readonly IPasswordRepository _passwordRepository;

    public NewPasswordCommandHandler(IPasswordRepository passwordRepository)
    {
        _passwordRepository = passwordRepository;
    }

    public async Task<ErrorOr<PasswordDto>> Handle(NewPasswordCommand request, CancellationToken cancellationToken)
    {
        Domain.Entities.Password entity = new()
        {
            Login = request.Login,
            Name = request.Name,
            Properties = request.Properties,
            Value = request.Value,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _passwordRepository.CreateAsync(entity);

        return entity.Adapt<PasswordDto>();
    }
}
