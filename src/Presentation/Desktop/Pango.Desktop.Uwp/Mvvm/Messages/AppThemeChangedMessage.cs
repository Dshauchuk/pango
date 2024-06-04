using CommunityToolkit.Mvvm.Messaging.Messages;
using Windows.UI.Xaml;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

/// <summary>
/// Message to notify app, that the application UI theme was changed
/// </summary>
internal class AppThemeChangedMessage : ValueChangedMessage<ElementTheme>
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="value">UI theme to which to which the application should be switched</param>
    public AppThemeChangedMessage(ElementTheme value) : base(value)
    {
    }
}