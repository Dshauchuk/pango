using ErrorOr;
using MediatR;
using Pango.Application.Common;
using Pango.Application.Common.Exceptions;
using Pango.Application.Common.Interfaces.Services;

namespace Pango.Application.UseCases.Data.Commands.Export;

public class ExportDataCommandHandler
    : IRequestHandler<ExportDataCommand, ErrorOr<ExportResult>>
{
    public readonly IDataExporter _dataExporter;

    public ExportDataCommandHandler(IDataExporter dataExporter)
    {
        _dataExporter = dataExporter;
    }

    public async Task<ErrorOr<ExportResult>> Handle(ExportDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            string path = await _dataExporter.ExportAsync(request.Contents, request.ExportOptions);
        
            return new ExportResult(path);
        }
        catch(PangoExportException pEx)
        {
            return Error.Failure(pEx.Code, pEx.Message);
        }
        catch (Exception ex)
        {
            return Error.Failure(ApplicationErrors.Data.ExportError, $"Export failed: {ex.Message}");
        }
    }
}
