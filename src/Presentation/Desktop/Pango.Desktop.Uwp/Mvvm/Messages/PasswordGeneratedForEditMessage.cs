using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

public class PasswordGeneratedForEditMessage : ValueChangedMessage<string>
{
    public PasswordGeneratedForEditMessage(string password) : base(password)
    {

    }
}

