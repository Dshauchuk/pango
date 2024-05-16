namespace Pango.Desktop.Uwp.Dialogs;

public interface IContentDialog
{
    string Title { get; }
    IDialogViewModel ViewModel { get; }
}
