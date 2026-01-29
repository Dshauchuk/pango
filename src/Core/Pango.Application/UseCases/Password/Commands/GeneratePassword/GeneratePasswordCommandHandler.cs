using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Application.UseCases.Password.Commands.GeneratePassword
{
    public class GeneratePasswordCommandHandler : IRequestHandler<GeneratePasswordCommand, ErrorOr<string>>
    {
        private readonly IPasswordGenerator _passwordGenerator;
        private readonly ILogger<GeneratePasswordCommandHandler> _logger;
        public GeneratePasswordCommandHandler(IPasswordGenerator passwordGenerator, ILogger<GeneratePasswordCommandHandler> logger)
        {
            _passwordGenerator = passwordGenerator;
            _logger = logger;
        }
        public async Task<ErrorOr<string>> Handle(GeneratePasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Length < PasswordConstants.MinLength || request.Length > PasswordConstants.MaxLength)
                {
                    return Error.Failure(
                        ApplicationErrors.Password.GenerationInvalidLength,
                        $"Password length must be between {PasswordConstants.MinLength} and {PasswordConstants.MaxLength} characters.");
                }

                if (!request.UseUppercase && !request.UseLowercase && !request.UseDigits && !request.UseSpecial)
                {
                    return Error.Failure(
                        ApplicationErrors.Password.GenerationInvalidCharsets,
                        "At least one character set must be selected.");
                }
                var options = new PasswordGenerationOptions(
                    request.Length,
                    request.UseUppercase,
                    request.UseLowercase,
                    request.UseDigits,
                    request.UseSpecial,
                    request.ExcludeAmbiguous
                );

                var password = await _passwordGenerator.GeneratePasswordAsync(options);
                return password;
            }
            catch (Exception ex)
            {
                //// to change
                _logger.LogError(ex,
                     "Password with length {PasswordLength} cannot be created: {Message}",
                     request.Length,
                     ex.Message);

                return Error.Failure(
                    ApplicationErrors.Password.CreationFailed,
                    $"Password with length {request.Length} cannot be created: {ex.Message}");
            }
        }
    }
}
