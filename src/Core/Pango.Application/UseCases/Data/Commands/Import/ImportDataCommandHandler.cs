using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;
using Pango.Domain.Entities;

namespace Pango.Application.UseCases.Data.Commands.Import;

public class ImportDataCommandHandler(
    IDataImporter dataImporter,
    IPasswordRepository passwordRepository,
    IRepositoryContextFactory repositoryContextFactory,
    IUserContextProvider userContextProvider,
    ILogger<ImportDataCommandHandler> logger) : IRequestHandler<ImportDataCommand, ErrorOr<ImportResult>>
{
    private readonly IDataImporter _dataImporter = dataImporter;
    private readonly IPasswordRepository _passwordRepository = passwordRepository;
    private readonly IRepositoryContextFactory _repositoryContextFactory = repositoryContextFactory;
    private readonly IUserContextProvider _userContextProvider = userContextProvider;
    private readonly ILogger _logger = logger;

    public async Task<ErrorOr<ImportResult>> Handle(ImportDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            ImportResultDto result = await _dataImporter.ImportAsync(request.SourcePath, request.Options);
            if (result is null) return Error.Failure(ApplicationErrors.Data.ImportError, "Import result is null");

            var context = _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync());
            var existingItems = (await _passwordRepository.QueryAsync(p => true, context)).ToList();

            foreach (IContentPackage package in result.ContentPackages)
            {
                if (package.ContentType == Domain.Enums.ContentType.Passwords)
                {
                    if (package.Data is not IEnumerable<PangoPassword> importedItems) continue;

                    List<PangoPassword> itemsToAdd = [];
                    foreach (var newItem in importedItems)
                    {
                        string newPath = newItem.CatalogPath ?? string.Empty;

                        bool alreadyExists = existingItems.Any(existing => {
                            bool samePath = (existing.CatalogPath ?? string.Empty).Equals(newPath, StringComparison.OrdinalIgnoreCase);
                            bool sameName = existing.Name.Equals(newItem.Name, StringComparison.OrdinalIgnoreCase);

                            if (!samePath || !sameName) return false;
                            if (newItem.IsCatalog != existing.IsCatalog) return false;
                            if (newItem.IsCatalog) return true;

                            return (existing.Login ?? string.Empty).Equals(newItem.Login ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                        });

                        if (!alreadyExists)
                        {
                            newItem.Id = Guid.NewGuid();
                            newItem.UserName = _userContextProvider.GetUserName();
                            itemsToAdd.Add(newItem);
                            existingItems.Add(newItem);
                        }
                    }

                    if (itemsToAdd.Any())
                    {
                        await _passwordRepository.CreateAsync(itemsToAdd, context);
                    }
                }
            }
            return new ImportResult(result.Manifest);
        }
        catch (PangoDataDecryptionException ex)
        {
            return Error.Failure(ex.Code, "Wrong password for the export file.");
        }
        catch (Exception ex)
        {
            return Error.Failure(ApplicationErrors.Data.ImportError, ex.Message);
        }
    }
}
