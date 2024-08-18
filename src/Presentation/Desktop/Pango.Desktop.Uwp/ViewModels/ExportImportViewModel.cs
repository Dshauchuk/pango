using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.ExportImport)]
public sealed class ExportImportViewModel : ViewModelBase
{
    public ExportImportViewModel(ILogger<ExportImportViewModel> logger) : base(logger)
    {

    }
}
