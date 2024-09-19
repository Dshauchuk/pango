namespace Pango.Application.Common.Interfaces.Persistence;

public interface IExportOptions
{
    public string Description { get; }
    public EncodingOptions EncodingOptions { get; }
}
