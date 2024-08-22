namespace Pango.Application.UseCases.Data.Commands.Export;

public class ExportResult
{
    public string Path { get; }

    public ExportResult(string path)
    {
        Path = path;
    }
}
