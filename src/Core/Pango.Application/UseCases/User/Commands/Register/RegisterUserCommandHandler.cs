using ErrorOr;
using Mapster;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.User.Commands.Register
{
    public class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, ErrorOr<PangoUserDto>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHashProvider _passwordHashProvider;

        public RegisterUserCommandHandler(IUserRepository userRepository, IPasswordHashProvider passwordHashProvider)
        {
            _userRepository = userRepository;
            _passwordHashProvider = passwordHashProvider;
        }

        public async Task<ErrorOr<PangoUserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            // TODO: has the password
            string passwordHash = _passwordHashProvider.Hash(request.Password, out _);

            PangoUser user = new()
            {
                UserName = request.UserName,
                MasterPasswordHash = passwordHash
            };

            await _userRepository.CreateAsync(user);

            return user.Adapt<PangoUserDto>();
        }
    }
}
