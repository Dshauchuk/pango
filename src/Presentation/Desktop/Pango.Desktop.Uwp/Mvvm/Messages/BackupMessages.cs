using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Pango.Desktop.Uwp.Mvvm.Messages;

/// <summary>
/// Message sent when backup settings are saved from the UI to notify the tray to start/stop the process.
/// </summary>
public class BackupStateChangedMessage(bool isEnabled) : ValueChangedMessage<bool>(isEnabled)
{
}

/// <summary>
/// Message sent from the Tray Icon to toggle the settings in the UI and JSON configuration.
/// </summary>
public class TrayBackupToggleMessage(bool isEnabled) : ValueChangedMessage<bool>(isEnabled)
{
}