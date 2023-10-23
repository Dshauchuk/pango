using CommunityToolkit.Mvvm.Messaging.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

public class NavigationRequstedMessage : ValueChangedMessage<NavigationParameters>
{
    public NavigationRequstedMessage(NavigationParameters value) : base(value)
    {

    }
}
