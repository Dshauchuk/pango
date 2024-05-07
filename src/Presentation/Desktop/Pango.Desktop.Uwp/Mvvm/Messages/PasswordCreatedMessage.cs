using CommunityToolkit.Mvvm.Messaging.Messages;
using Pango.Application.Models;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

internal class PasswordCreatedMessage : ValueChangedMessage<PangoPasswordListItemDto>
{
    public PasswordCreatedMessage(PangoPasswordListItemDto value) : base(value)
    {
    }
}
