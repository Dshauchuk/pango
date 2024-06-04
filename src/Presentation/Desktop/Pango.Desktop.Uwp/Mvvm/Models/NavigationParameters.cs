using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Models.Parameters;

namespace Pango.Desktop.Uwp.Mvvm.Models;

public class NavigationParameters(AppView navigatedView, INavigationParameter value = null)
{
    public AppView NavigatedView { get; } = navigatedView;

    public INavigationParameter Value { get; } = value;
}
