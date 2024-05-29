using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

/// <summary>
/// Message to notify app, that the autolock idle was changed
/// </summary>
public class AutolockIdleChangedMessage : ValueChangedMessage<int?>
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="value">New value of a autolock idle. If null - autolock on idle is disabled</param>
    public AutolockIdleChangedMessage(int? value) : base(value)
    {
    }
}