using Pango.Desktop.Uwp.Core.Enums;

namespace Pango.Desktop.Uwp.Mvvm.Models;

internal class InAppNotificationMessage
{
    public InAppNotificationMessage(string codeOrMessage, AppNotificationType type = AppNotificationType.Success)
    {
        Message = codeOrMessage;
        Type = type;
    }

    public string Message { get; }

    public AppNotificationType Type { get; }
}
