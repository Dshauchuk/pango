using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

/// <summary>
/// Message for local tab switching within the password screen (0 - List, 1 - Edit Form)
/// </summary>
public class SwitchPasswordTabMessage(int value) : ValueChangedMessage<int>(value)
{
}