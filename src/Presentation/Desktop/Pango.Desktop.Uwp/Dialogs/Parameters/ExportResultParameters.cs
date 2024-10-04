using Pango.Application.UseCases.Data.Commands.Export;

namespace Pango.Desktop.Uwp.Dialogs.Parameters;

public class ExportResultParameters : IDialogParameter
{
    public ExportResultParameters(ExportResult exportResult)
    {
        ExportResult = exportResult;
    }

    public ExportResult ExportResult { get; }
}
