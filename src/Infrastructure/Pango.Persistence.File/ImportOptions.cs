using Pango.Application.Common;
using Pango.Application.Common.Interfaces.Persistence;

namespace Pango.Persistence.File;

public class ImportOptions : IImportOptions
{
    public ImportOptions(EncodingOptions encodingOptions)
    {
        EncodingOptions = encodingOptions;
    }

    public EncodingOptions EncodingOptions { get; }
}
