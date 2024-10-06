using ErrorOr;
using MediatR;
using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Application.UseCases.Data.Commands.Import;

public class ImportDataCommand : IRequest<ErrorOr<ImportResult>>
{
    public ImportDataCommand(string sourcePath, IImportOptions options)
    {
        SourcePath = sourcePath;
        Options = options;
    }

    public string SourcePath { get; }
    
    public IImportOptions Options { get; }
}
