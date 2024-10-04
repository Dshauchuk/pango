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

public class ImportDataCommandHandler
    : IRequestHandler<ImportDataCommand, ErrorOr<ImportResult>>
{
    private readonly IDataImporter _dataImporter;
    private readonly IPasswordRepository _passwordRepository;
    private readonly IRepositoryContextFactory _repositoryContextFactory;
    private readonly IUserContextProvider _userContextProvider;
    private readonly ILogger _logger;

    public ImportDataCommandHandler(
        IDataImporter dataImporter, 
        IPasswordRepository passwordRepository, 
        IRepositoryContextFactory repositoryContextFactory, 
        IUserContextProvider userContextProvider,
        ILogger<ImportDataCommandHandler> logger)
    {
        _logger = logger;
        _dataImporter = dataImporter;
        _passwordRepository = passwordRepository;
        _repositoryContextFactory = repositoryContextFactory;
        _userContextProvider = userContextProvider;
    }

    public async Task<ErrorOr<ImportResult>> Handle(ImportDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            ImportResultDto result = await _dataImporter.ImportAsync(request.SourcePath, request.Options);

            if(result is null)
            {
                return Error.Failure(ApplicationErrors.Data.ImportError, "Import result is null");   
            }
            else
            {
                foreach (IContentPackage package in result.ContentPackages)
                {
                    if(package.ContentType == Domain.Enums.ContentType.Passwords)
                    {
                        var passwords = package.Data as IEnumerable<PangoPassword>;

                        if(passwords is not null && passwords.Any())
                        {
                            // re-generate the ID to avoid collisions
                            foreach (var item in passwords)
                            {
                                item.Id = Guid.NewGuid();
                            }

                            await _passwordRepository.CreateAsync(passwords, _repositoryContextFactory.Create(_userContextProvider.GetUserName(), await _userContextProvider.GetEncodingOptionsAsync()));
                        }
                    }
                    else
                    {
                        throw new NotImplementedException($"Cannot parse {package.ContentType.ToString()} type. Not implemented.");
                    }
                }
            }

            return new ImportResult(result.Manifest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data import failed: {message}", ex.Message);

            if(ex is PangoException pEx)
            {
                return Error.Failure(pEx.Code, pEx.Message);
            }

            return Error.Failure(ApplicationErrors.Data.ImportError, $"Import failed: {ex.Message}");
        }
    }
}
