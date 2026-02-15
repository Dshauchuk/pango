using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

public class CreatePasswordFromGeneratorMessage : ValueChangedMessage<string>
{
    public CreatePasswordFromGeneratorMessage(string value) : base(value)
    {
    }
}
