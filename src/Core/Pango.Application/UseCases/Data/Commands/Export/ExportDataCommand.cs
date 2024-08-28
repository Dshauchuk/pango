using ErrorOr;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Application.UseCases.Data.Commands.Export;

public class ExportDataCommand : IRequest<ErrorOr<ExportResult>>
{
    public ExportDataCommand(List<ExportItem> exportItems, IExportOptions exportOptions)
    {
        ExportOptions = exportOptions;
        ExportItems = exportItems;
    }

    public List<ExportItem> ExportItems { get; }

    public IExportOptions ExportOptions { get; }

}
