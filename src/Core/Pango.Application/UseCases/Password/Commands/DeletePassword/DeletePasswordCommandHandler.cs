using ErrorOr;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Application.UseCases.Password.Commands.DeletePassword;

public class DeletePasswordCommandHandler
    : IRequestHandler<DeletePasswordCommand, ErrorOr<bool>>
{
    private readonly IPasswordRepository _passwordRepository;

    public DeletePasswordCommandHandler(IPasswordRepository passwordRepository)
    {
        _passwordRepository = passwordRepository;
    }

    public async Task<ErrorOr<bool>> Handle(DeletePasswordCommand request, CancellationToken cancellationToken)
    {
        var password = await _passwordRepository.FindAsync(pwd => pwd.Id == request.PasswordId);
    
        if(password is not null)
        {
            await _passwordRepository.DeleteAsync(password);
            
            return true;
        }

        return false;
    }
}
