using CommunityToolkit.Mvvm.Messaging.Messages;
using Pango.Application.Models;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

public class PasswordUpdatedMessage : ValueChangedMessage<PangoPasswordListItemDto>
{
    public PasswordUpdatedMessage(PangoPasswordListItemDto value) : base(value)
    {
    }
}
