using Microsoft.Extensions.Logging;
using Pango.Desktop.Uwp.Core.Attributes;
using Pango.Desktop.Uwp.Core.Enums;

namespace Pango.Desktop.Uwp.ViewModels;

[AppView(AppView.MainAppView)]
public sealed class MainAppViewModel : ViewModelBase
{
    public MainAppViewModel(ILogger<MainAppViewModel> logger) : base(logger)
    {
            
    }
}
