using Pango.Desktop.Uwp.Core.Enums;

namespace Pango.Desktop.Uwp.Mvvm.Models;

public class NavigationParameters
{
    public NavigationParameters(AppView navigatedView, object value = null)
    {
        NavigatedView = navigatedView;
        Value = value;
    }

    public AppView NavigatedView { get; }

    public object Value { get; }
}
