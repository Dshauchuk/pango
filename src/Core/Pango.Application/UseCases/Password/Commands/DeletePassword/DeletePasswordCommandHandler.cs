using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.Password.Commands.DeletePassword;

public class DeletePasswordCommandHandler(
    IPasswordRepository passwordRepository,
    IUserContextProvider userContextProvider,
    IRepositoryContextFactory repositoryContextFactory,
    ILogger<DeletePasswordCommandHandler> logger)
        : IRequestHandler<DeletePasswordCommand, ErrorOr<bool>>
{
    private readonly IPasswordRepository _passwordRepository = passwordRepository;
    private readonly IUserContextProvider _userContextProvider = userContextProvider;
    private readonly IRepositoryContextFactory _repositoryContextFactory = repositoryContextFactory;
    private readonly ILogger<DeletePasswordCommandHandler> _logger = logger;

    public async Task<ErrorOr<bool>> Handle(DeletePasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            IRepositoryActionContext context = _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync());

            var password = await _passwordRepository.FindAsync(pwd => pwd.Id == request.PasswordId, context);

            if (password is null)
            {
                return Error.Failure(ApplicationErrors.Password.NotFound, $"Password with id {request.PasswordId} cannot be deleted: password not found");
            }

            List<PangoPassword> passwordsToRemove = [password];

            if (password.IsCatalog)
            {
                // Construct the full path of the catalog being deleted
                string catalogPath = string.IsNullOrEmpty(password.CatalogPath) ? password.Name : $"{password.CatalogPath}{AppConstants.CatalogDelimeter}{password.Name}";

                // Construct the prefix for sub-items (e.g., "Parent/Child/")
                string childPrefix = $"{catalogPath}{AppConstants.CatalogDelimeter}";

                // Query: Find exact children OR any descendants (recursive check)
                var internalPasswords = (await _passwordRepository.QueryAsync(p =>
                    p.CatalogPath == catalogPath ||
                    p.CatalogPath.StartsWith(childPrefix), context)).ToList();

                if (internalPasswords.Count != 0)
                {
                    passwordsToRemove.AddRange(internalPasswords);
                }
            }

            foreach (var pwd in passwordsToRemove)
            {
                await _passwordRepository.DeleteAsync(pwd, context);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password with id {PasswordId} cannot be deleted: {Message}", request.PasswordId, ex.Message);
            return Error.Failure(ApplicationErrors.Password.DeletionFailed, $"Password with id {request.PasswordId} cannot be deleted: {ex.Message}");
        }
    }
}
