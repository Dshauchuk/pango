using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.Password.Commands.DeleteUserPasswords;

public class DeleteUserPasswordsCommandHandler
    : IRequestHandler<DeleteUserPasswordsCommand, ErrorOr<bool>>
{
    private readonly IPasswordRepository _passwordRepository;
    private readonly IUserContextProvider _userContextProvider;
    private readonly IRepositoryContextFactory _repositoryContextFactory;
    private readonly ILogger<DeleteUserPasswordsCommandHandler> _logger;

    public DeleteUserPasswordsCommandHandler(IPasswordRepository passwordRepository, IUserContextProvider userContextProvider, ILogger<DeleteUserPasswordsCommandHandler> logger, IRepositoryContextFactory repositoryContextFactory)
    {
        _passwordRepository = passwordRepository;
        _userContextProvider = userContextProvider;
        _repositoryContextFactory = repositoryContextFactory;
        _logger = logger;
    }

    public async Task<ErrorOr<bool>> Handle(DeleteUserPasswordsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            IRepositoryActionContext context = _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync());

            IEnumerable<PangoPassword> userPasswords = await _passwordRepository.QueryAsync(p => true, context);

            if (userPasswords?.Any() == true)
            {
                foreach (PangoPassword password in userPasswords)
                {
                    await _passwordRepository.DeleteAsync(password, context);
                }

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Passwords deletion failed");
            return Error.Failure(ApplicationErrors.Password.DeletionFailed, "Passwords deletion failed");
        }
    }
}
