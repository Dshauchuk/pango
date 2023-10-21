using CommunityToolkit.Mvvm.Messaging.Messages;
using Pango.Desktop.Uwp.Core.Enums;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

public class NavigationRequstedMessage : ValueChangedMessage<AppView>
{
    public NavigationRequstedMessage(AppView value) : base(value)
    {

    }
}
