namespace Pango.Application.Common.Interfaces.Persistence;

public interface IExportOptions
{
    public string Owner { get; }
    public EncodingOptions EncodingOptions { get; }
}
