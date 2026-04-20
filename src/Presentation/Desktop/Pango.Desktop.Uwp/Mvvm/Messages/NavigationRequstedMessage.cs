using CommunityToolkit.Mvvm.Messaging.Messages;
using Pango.Desktop.Uwp.Mvvm.Models;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

public class NavigationRequestedMessage(NavigationParameters value) : ValueChangedMessage<NavigationParameters>(value)
{
}
