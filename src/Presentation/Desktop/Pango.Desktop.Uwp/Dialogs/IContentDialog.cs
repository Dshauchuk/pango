using Windows.UI.Xaml.Controls;

namespace Pango.Desktop.Uwp.Dialogs;

public interface IContentDialog
{
    string Title { get; }
    IDialogViewModel ViewModel { get; }
    void DialogOpened(ContentDialog sender, ContentDialogOpenedEventArgs args);
}
