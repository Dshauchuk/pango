using Pango.Desktop.Uwp.Core.Enums;
using Pango.Desktop.Uwp.Models.Parameters;

namespace Pango.Desktop.Uwp.Mvvm.Models;

public class NavigationParameters(AppView navigatedView, AppView sourceView, INavigationParameter? value = null)
{
    public AppView NavigatedView { get; } = navigatedView;
    public AppView SourceView { get; } = sourceView;
    public INavigationParameter? Parameter { get; } = value;
}
