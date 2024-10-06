using Microsoft.UI.Xaml.Controls;

namespace Pango.Desktop.Uwp.Dialogs;

public interface IContentDialog
{
    string Title { get; }

    IDialogViewModel? ViewModel { get; }

    string? PrimaryButtonText { get; }

    string? CancelButtonText { get; }

    object? GetDialogParameter();

    void DialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args);
}
