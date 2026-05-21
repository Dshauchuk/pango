using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

/// <summary>
/// Internal message to notify the app that the expiration cache has been updated on disk.
/// </summary>
public class ExpirationCacheUpdatedMessage : ValueChangedMessage<bool>
{
    public ExpirationCacheUpdatedMessage() : base(true)
    {
    }
}
