using ErrorOr;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Application.UseCases.Data.Commands.Import;

public class ImportDataCommand(string sourcePath, IImportOptions options, List<Guid>? selectedIds = null, bool importToSeparateFolder = false) : IRequest<ErrorOr<ImportResult>>
{
    public string SourcePath { get; } = sourcePath;
    public IImportOptions Options { get; } = options;
    public List<Guid>? SelectedIds { get; } = selectedIds;
    public bool ImportToSeparateFolder { get; } = importToSeparateFolder;
}
