using Pango.Domain.Enums;

namespace Pango.Application.UseCases.Data.Commands.Export;

public readonly record struct ExportItem
{
    public ExportItem(ContentType contentType, Guid id) : this()
    {
        ContentType = contentType;
        Id = id;
    }

    public Guid Id { get; }
    public ContentType ContentType { get; }
}
