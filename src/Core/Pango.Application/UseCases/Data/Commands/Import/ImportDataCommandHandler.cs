using ErrorOr;
using MediatR;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Services;
using Pango.Application.Models;

namespace Pango.Application.UseCases.Data.Commands.Import;

public class ImportDataCommandHandler
    : IRequestHandler<ImportDataCommand, ErrorOr<ImportResult>>
{
    private readonly IDataImporter _dataImporter;

    public ImportDataCommandHandler(IDataImporter dataImporter)
    {
        _dataImporter = dataImporter;
    }

    public async Task<ErrorOr<ImportResult>> Handle(ImportDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            ImportResultDto result = await _dataImporter.ImportAsync(request.SourcePath, request.Options);

            if(result is null)
            {

            }

            return new ImportResult(result.Manifest, result.ContentPackages);
        }
        catch (PangoImportException pEx)
        {
            return Error.Failure(pEx.Code, pEx.Message);
        }
        catch (Exception ex)
        {
            return Error.Failure(ApplicationErrors.Data.ImportError, $"Import failed: {ex.Message}");
        }
    }
}
