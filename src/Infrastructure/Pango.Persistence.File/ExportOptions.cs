using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Persistence.File;

public class ExportOptions : IExportOptions
{
    public ExportOptions(string owner, EncodingOptions encodingOptions)
    {
        EncodingOptions = encodingOptions;
        Description = owner;
    }

    public EncodingOptions EncodingOptions { get; }
    public string Description { get; }
}
