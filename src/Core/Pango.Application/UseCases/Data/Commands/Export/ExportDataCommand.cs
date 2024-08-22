using ErrorOr;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Application.UseCases.Data.Commands.Export;

public class ExportDataCommand : IRequest<ErrorOr<ExportResult>>
{
    public ExportDataCommand(List<IContentPackage> contents, IExportOptions exportOptions)
    {
        Contents = contents;
        ExportOptions = exportOptions;
    }

    public List<IContentPackage> Contents { get; }

    public IExportOptions ExportOptions { get; }

}
