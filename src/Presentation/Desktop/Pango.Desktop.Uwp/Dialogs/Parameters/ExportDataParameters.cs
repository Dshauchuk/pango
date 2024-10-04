using Pango.Application.UseCases.Data.Commands.Export;
using System.Collections.Generic;

namespace Pango.Desktop.Uwp.Dialogs.Parameters;

public class ExportDataParameters(List<ExportItem> items)
    : IDialogParameter
{
    public List<ExportItem> Items { get; } = items;
}
