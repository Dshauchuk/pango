using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.Shell)]
public class ShellViewModel : ViewModelBase
{
    public ShellViewModel(ILogger<ShellViewModel> logger) : base(logger)
    {
    }
}
