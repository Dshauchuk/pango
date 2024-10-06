
using Pango.Domain.Enums;

namespace Pango.Application.UseCases.Data.Commands.Export;

public class ExportResult
{
    public string Path { get; }
    public Dictionary<ContentType, int> Contents { get; }
    public DateTime GeneratedAt { get; }
    public string AppVersion { get; }

    public ExportResult(string path, Dictionary<ContentType, int> contents, DateTime generatedAt, string appVersion)
    {
        Path = path;
        Contents = contents;
        GeneratedAt = generatedAt;
        AppVersion = appVersion;
    }
}
