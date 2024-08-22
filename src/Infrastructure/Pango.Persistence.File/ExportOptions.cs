using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Persistence.File;

public class ExportOptions : IExportOptions
{
    public ExportOptions(EncodingOptions encodingOptions)
    {
        EncodingOptions = encodingOptions;
    }

    public EncodingOptions EncodingOptions { get; }

}
