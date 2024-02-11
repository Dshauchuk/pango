using ErrorOr;
using MediatR;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Models;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.Password.Commands.UpdatePassword;

public class UpdatePasswordCommandHandler
: IRequestHandler<UpdatePasswordCommand, ErrorOr<PangoPasswordDto>>
{
    private readonly IPasswordRepository _passwordRepository;

    public UpdatePasswordCommandHandler(IPasswordRepository passwordRepository)
    {
        _passwordRepository = passwordRepository;
    }

    public async Task<ErrorOr<PangoPasswordDto>> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
    {
        PangoPassword password = await _passwordRepository.FindAsync(p => p.Id == request.PasswordId);

        if(password is null)
        {
            throw new PasswordNotFoundException($"Password with id {request.PasswordId} not found");
        }

        password.Name = request.Name;
        password.Value = request.Value;
        password.Login = request.Login;

        Dictionary<string, string> props = password.Properties ?? new();

        if (!props.ContainsKey(PasswordProperties.Notes))
        {

        }

        //Domain.Entities.PangoPassword entity = new()
        //{
        //    Login = request.Login,
        //    Name = request.Name,
        //    Properties = request.Properties,
        //    Value = request.Value,
        //    CreatedAt = DateTimeOffset.UtcNow
        //};

        //await _passwordRepository.CreateAsync(entity);

        //return entity.Adapt<PangoPasswordDto>();

        return new PangoPasswordDto();
    }
}
